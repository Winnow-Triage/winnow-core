using Microsoft.EntityFrameworkCore;
using Winnow.Server.Entities;
using Winnow.Server.Features.Shared;
using Winnow.Server.Infrastructure.MultiTenancy;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Infrastructure.Scheduling;

public class ClusterRefinementJob(
    IServiceScopeFactory scopeFactory,
    ILogger<ClusterRefinementJob> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var tenants = GetAllActiveTenants();

                foreach (var tenantId in tenants)
                {
                    logger.LogInformation("Janitor: Starting cleanup for Tenant {TenantId}", tenantId);
                    try
                    {
                        await RunRefinementForTenantAsync(tenantId, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Janitor: Refinement failed for Tenant {TenantId}", tenantId);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Cluster refinement cycle failed.");
            }

            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }

    private List<string> GetAllActiveTenants()
    {
        var dataDir = Path.Combine(Directory.GetCurrentDirectory(), "Data");
        if (!Directory.Exists(dataDir)) return new List<string> { "default" };

        var files = Directory.GetFiles(dataDir, "*.db");
        var tenants = files
            .Select(f => Path.GetFileNameWithoutExtension(f))
            .ToList();

        if (!tenants.Contains("default")) tenants.Add("default");
        return tenants;
    }

    private async Task RunRefinementForTenantAsync(string tenantId, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();

        var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
        if (tenantContext is TenantContext concreteContext)
        {
            concreteContext.TenantId = tenantId == "default" ? null : tenantId;
        }

        var db = scope.ServiceProvider.GetRequiredService<WinnowDbContext>();

        // Ensure vector table exists for this tenant
        await db.Database.ExecuteSqlRawAsync(
            "CREATE VIRTUAL TABLE IF NOT EXISTS vec_reports USING vec0(embedding float[384] distance_metric=cosine);",
            ct);

        var recentLeaders = await db.Reports
            .AsNoTracking()
            .Where(t => t.ParentReportId == null
                     && t.Status != "Duplicate")
            .OrderBy(t => t.CreatedAt) 
            .ToListAsync(ct);

        if (recentLeaders.Count < 1) return;

        logger.LogInformation("Janitor [{TenantId}]: Scanning {Count} active leaders.", tenantId, recentLeaders.Count);

        var embeddingService = scope.ServiceProvider.GetRequiredService<Winnow.Server.Services.Ai.IEmbeddingService>();
        foreach (var leader in recentLeaders.Where(l => l.Embedding == null))
        {
            try
            {
                logger.LogInformation("Janitor [{TenantId}]: Generating missing embedding for report {Id}", tenantId, leader.Id);
                var text = $"{leader.Message}\n{leader.StackTrace}";
                var embeddingFloats = await embeddingService.GetEmbeddingAsync(text);
                var embeddingBytes = new byte[embeddingFloats.Length * sizeof(float)];
                Buffer.BlockCopy(embeddingFloats, 0, embeddingBytes, 0, embeddingBytes.Length);

                await db.Database.ExecuteSqlRawAsync(
                    "UPDATE Reports SET Embedding = {0} WHERE Id = {1}",
                    [embeddingBytes, leader.Id], ct);

                await db.Database.ExecuteSqlRawAsync(
                    "INSERT OR REPLACE INTO vec_reports(rowid, embedding) VALUES ((SELECT rowid FROM Reports WHERE Id = {0}), {1})",
                    [leader.Id, embeddingBytes], ct);

                leader.Embedding = embeddingBytes;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Janitor [{TenantId}]: Failed to heal embedding for {Id}", tenantId, leader.Id);
            }
        }

        int mergeCount = 0;
        int suggestCount = 0;
        var processedIds = new HashSet<Guid>();
        var mergeMap = new Dictionary<Guid, Guid>(); 
        
        var duplicateChecker = scope.ServiceProvider.GetRequiredService<Winnow.Server.Services.Ai.IDuplicateChecker>();
        var negativeCache = scope.ServiceProvider.GetRequiredService<Winnow.Server.Services.Ai.INegativeMatchCache>();

        foreach (var leaderA in recentLeaders)
        {
            if (processedIds.Contains(leaderA.Id)) continue;
            if (leaderA.Embedding == null) continue;

            const double HardMergeThreshold = 0.35;
            const double SuggestThreshold = 0.55;

            var matches = await FindMatchesInDb(db, leaderA, SuggestThreshold, ct);
            if (matches.Count == 0) continue;

            var matchIds = matches.Select(m => m.Id).ToList();
            var parentInfo = await db.Reports
                .AsNoTracking()
                .Where(t => matchIds.Contains(t.Id))
                .Select(t => new { t.Id, t.ParentReportId, t.CreatedAt })
                .ToDictionaryAsync(t => t.Id, t => new { t.ParentReportId, t.CreatedAt }, ct);

            var clusterGroupsRaw = matches
                .Select(m =>
                {
                    var info = parentInfo[m.Id];
                    var currentParentId = info.ParentReportId ?? m.Id;

                    while (mergeMap.TryGetValue(currentParentId, out var nextParent))
                    {
                        currentParentId = nextParent;
                    }

                    return new { Match = m, UltimateParentId = currentParentId };
                })
                .Where(m => m.UltimateParentId != leaderA.Id) 
                .GroupBy(m => m.UltimateParentId)
                .ToList();

            var clusterGroups = new List<(Guid Key, List<ReportMatch> Items)>();
            foreach (var group in clusterGroupsRaw)
            {
                var trueRootId = await db.ResolveUltimateMasterAsync(group.Key, ct);
                clusterGroups.Add((trueRootId, group.Select(g => g.Match).ToList()));
            }

            ClusterMatch? bestMatch = null;
            float[] leaderAFloats = BytesToFloats(leaderA.Embedding);

            foreach (var group in clusterGroups)
            {
                var clusterId = group.Key;

                var members = await db.Reports
                    .AsNoTracking()
                    .Where(t => t.Id == clusterId || t.ParentReportId == clusterId)
                    .Select(t => t.Embedding)
                    .Where(e => e != null)
                    .ToListAsync(ct);

                if (members.Count == 0) continue;

                var centroid = CalculateCentroid(members);
                var centroidDist = CalculateCosineDistance(leaderAFloats, centroid);

                if (bestMatch == null || centroidDist < bestMatch.Distance)
                {
                    bestMatch = new ClusterMatch(clusterId, group.Items.First().Message, centroidDist);
                }
            }

            if (bestMatch != null)
            {
                var targetReport = await db.Reports.AsNoTracking()
                    .Where(t => t.Id == bestMatch.Id)
                    .Select(t => new { t.Id, t.Message, t.StackTrace, t.CreatedAt })
                    .FirstOrDefaultAsync(ct);

                if (targetReport == null) continue;

                if (bestMatch.Distance <= HardMergeThreshold)
                {
                    if (targetReport.CreatedAt > leaderA.CreatedAt)
                    {
                        continue;
                    }

                    if (bestMatch.Distance > 0.15)
                    {
                        if (negativeCache.IsKnownMismatch(tenantId, leaderA.Id, targetReport.Id))
                        {
                            continue;
                        }

                        var areDuplicates = await duplicateChecker.AreDuplicatesAsync(
                            leaderA.Message, leaderA.StackTrace!,
                            targetReport.Message, targetReport.StackTrace!,
                            ct);

                        if (!areDuplicates)
                        {
                            negativeCache.MarkAsMismatch(tenantId, leaderA.Id, targetReport.Id);
                            goto SuggestionPath;
                        }
                    }

                    await db.Database.ExecuteSqlRawAsync(
                        "UPDATE Reports SET ParentReportId = {0} WHERE ParentReportId = {1}",
                        [bestMatch.Id, leaderA.Id], ct);

                    var reportA = await db.Reports.FindAsync([leaderA.Id], ct);
                    if (reportA != null)
                    {
                        reportA.ParentReportId = bestMatch.Id;
                        reportA.Status = "Duplicate";
                        reportA.ConfidenceScore = (float)Math.Max(0, 1.0 - bestMatch.Distance);
                        reportA.SuggestedParentId = null;
                        reportA.SuggestedConfidenceScore = null;
                        mergeCount++;
                        processedIds.Add(leaderA.Id);
                        mergeMap[leaderA.Id] = bestMatch.Id; 
                    }
                    continue; 
                }

                SuggestionPath:
                if (bestMatch.Distance <= SuggestThreshold)
                {
                    if (negativeCache.IsKnownMismatch(tenantId, leaderA.Id, bestMatch.Id))
                    {
                        continue;
                    }

                    var reportA = await db.Reports.FindAsync([leaderA.Id], ct);
                    if (reportA != null && reportA.SuggestedParentId == null)
                    {
                        reportA.SuggestedParentId = bestMatch.Id;
                        reportA.SuggestedConfidenceScore = (float)Math.Max(0, 1.0 - bestMatch.Distance);
                        suggestCount++;
                    }
                }
            }
        }

        await BreakCyclesAsync(db, tenantId, ct);

        if (mergeCount > 0 || suggestCount > 0)
        {
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Janitor [{TenantId}]: Cleanup complete. Merged: {Merge}, Suggested: {Suggest}.",
                tenantId, mergeCount, suggestCount);
        }
    }

    private async Task BreakCyclesAsync(WinnowDbContext db, string tenantId, CancellationToken ct)
    {
        var candidates = await db.Reports
            .Where(t => t.ParentReportId != null)
            .Select(t => new { t.Id, t.ParentReportId })
            .ToListAsync(ct);

        int fixes = 0;
        foreach (var c in candidates)
        {
            var path = new List<Guid> { c.Id };
            var currentId = c.ParentReportId;

            while (currentId != null)
            {
                if (path.Contains(currentId.Value))
                {
                    logger.LogWarning("Janitor [{TenantId}]: Circular reference detected! Breaking cycle at {Id}", tenantId, currentId.Value);

                    await db.Database.ExecuteSqlRawAsync(
                        "UPDATE Reports SET ParentReportId = NULL, Status = 'Open' WHERE Id = {0}",
                        [currentId.Value], ct);

                    fixes++;
                    break;
                }

                path.Add(currentId.Value);

                var nextParentId = await db.Reports
                    .Where(t => t.Id == currentId.Value)
                    .Select(t => t.ParentReportId)
                    .FirstOrDefaultAsync(ct);

                if (nextParentId == null) break; 

                if (path.Count >= 2)
                {
                    logger.LogInformation("Janitor [{TenantId}]: Flattening deep hierarchy {A} -> {B} -> {Root}",
                        tenantId, path[0], path[1], nextParentId);

                    await db.Database.ExecuteSqlRawAsync(
                        "UPDATE Reports SET ParentReportId = {0} WHERE Id = {1}",
                        [nextParentId, path[0]], ct);

                    fixes++;
                    break;
                }

                currentId = nextParentId;
            }
        }

        if (fixes > 0)
        {
            logger.LogInformation("Janitor [{TenantId}]: Cycle Breaker fixed {Count} hierarchies.", tenantId, fixes);
        }
    }

    private async Task<List<ReportMatch>> FindMatchesInDb(WinnowDbContext db, Report target, double threshold, CancellationToken ct)
    {
        var sql = @"
            SELECT t.Id, t.Message, t.CreatedAt, v.distance as Distance
            FROM vec_reports v
            JOIN Reports t ON v.rowid = t.rowid
            WHERE v.embedding MATCH {0}
              AND k = 20
              AND v.distance < {1}
              AND t.Id != {2}
              AND t.Status != 'Duplicate'
        ";

        return await db.Database.SqlQueryRaw<ReportMatch>(sql, target.Embedding!, threshold, target.Id)
            .ToListAsync(ct);
    }

    private float[] BytesToFloats(byte[] bytes)
    {
        float[] floats = new float[bytes.Length / sizeof(float)];
        Buffer.BlockCopy(bytes, 0, floats, 0, bytes.Length);
        return floats;
    }

    private float[] CalculateCentroid(List<byte[]?> embeddings)
    {
        var validOnes = embeddings.Where(e => e != null).Cast<byte[]>().ToList();
        if (validOnes.Count == 0) return Array.Empty<float>();

        int length = validOnes[0].Length / sizeof(float);
        float[] centroid = new float[length];
        foreach (var bytes in validOnes)
        {
            for (int i = 0; i < length; i++)
            {
                centroid[i] += BitConverter.ToSingle(bytes, i * sizeof(float));
            }
        }
        for (int i = 0; i < length; i++) centroid[i] /= validOnes.Count;
        return centroid;
    }

    private double CalculateCosineDistance(float[] a, float[] b)
    {
        if (a.Length == 0 || b.Length == 0) return 1.0;
        float dot = 0, ma = 0, mb = 0;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            ma += a[i] * a[i];
            mb += b[i] * b[i];
        }
        if (ma == 0 || mb == 0) return 1.0;
        return 1.0 - (dot / (Math.Sqrt(ma) * Math.Sqrt(mb)));
    }

    private record ReportMatch(Guid Id, string Message, DateTime CreatedAt, double Distance);
    private record ClusterMatch(Guid Id, string Message, double Distance);
}
