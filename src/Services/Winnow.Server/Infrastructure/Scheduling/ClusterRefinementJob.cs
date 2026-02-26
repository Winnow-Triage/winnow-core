using Microsoft.EntityFrameworkCore;
using Winnow.Server.Domain.Services;
using Winnow.Server.Entities;
using Winnow.Server.Features.Shared;
using Winnow.Server.Infrastructure.MultiTenancy;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Infrastructure.Scheduling;

internal sealed class ClusterRefinementJob(
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

    private static List<string> GetAllActiveTenants()
    {
        var dataDir = Path.Combine(Directory.GetCurrentDirectory(), "Data");
        if (!Directory.Exists(dataDir)) return ["default"];

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

        // Get all projects for this tenant
        var projects = await db.Projects
            .AsNoTracking()
            .Select(p => p.Id)
            .ToListAsync(ct);

        var embeddingService = scope.ServiceProvider.GetRequiredService<Services.Ai.IEmbeddingService>();
        var duplicateChecker = scope.ServiceProvider.GetRequiredService<Services.Ai.IDuplicateChecker>();
        var negativeCache = scope.ServiceProvider.GetRequiredService<Services.Ai.INegativeMatchCache>();
        var vectorCalculator = scope.ServiceProvider.GetRequiredService<IVectorCalculator>();

        foreach (var projectId in projects)
        {
            await ProcessProjectAsync(db, projectId, tenantId, embeddingService, duplicateChecker, negativeCache, vectorCalculator, ct);
        }
    }

    private async Task ProcessProjectAsync(
        WinnowDbContext db,
        Guid projectId,
        string tenantId,
        Services.Ai.IEmbeddingService embeddingService,
        Services.Ai.IDuplicateChecker duplicateChecker,
        Services.Ai.INegativeMatchCache negativeCache,
        IVectorCalculator vectorCalculator,
        CancellationToken ct)
    {
        var recentLeaders = await db.Reports
            .AsNoTracking()
            .Where(t => t.ProjectId == projectId
                     && t.ParentReportId == null
                     && t.Status != "Duplicate")
            .OrderBy(t => t.CreatedAt)
            .ToListAsync(ct);

        if (recentLeaders.Count < 1) return;

        logger.LogInformation("Janitor [{TenantId}][Project: {ProjectId}]: Scanning {Count} active leaders.",
            tenantId, projectId, recentLeaders.Count);

        foreach (var leader in recentLeaders.Where(l => l.Embedding == null))
        {
            try
            {
                logger.LogInformation("Janitor [{TenantId}][Project: {ProjectId}]: Generating missing embedding for report {Id}",
                    tenantId, projectId, leader.Id);
                var text = $"{leader.Title}\n{leader.Message}\n{leader.StackTrace}";
                var embeddingFloats = await embeddingService.GetEmbeddingAsync(text);
                var embeddingBytes = new byte[embeddingFloats.Length * sizeof(float)];
                Buffer.BlockCopy(embeddingFloats, 0, embeddingBytes, 0, embeddingBytes.Length);

                await db.Database.ExecuteSqlRawAsync(
                    "UPDATE Reports SET Embedding = {0} WHERE Id = {1} AND ProjectId = {2}",
                    [embeddingBytes, leader.Id, projectId], ct);

                await db.Database.ExecuteSqlRawAsync(
                    "DELETE FROM vec_reports WHERE rowid = (SELECT rowid FROM Reports WHERE Id = {0})",
                    [leader.Id], ct);

                await db.Database.ExecuteSqlRawAsync(
                    "INSERT INTO vec_reports(rowid, embedding) VALUES ((SELECT rowid FROM Reports WHERE Id = {0} AND ProjectId = {1}), {2})",
                    [leader.Id, projectId, embeddingBytes], ct);

                leader.Embedding = embeddingBytes;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Janitor [{TenantId}][Project: {ProjectId}]: Failed to heal embedding for {Id}",
                    tenantId, projectId, leader.Id);
            }
        }

        int mergeCount = 0;
        int suggestCount = 0;
        var processedIds = new HashSet<Guid>();
        var mergeMap = new Dictionary<Guid, Guid>();

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
                .Where(t => matchIds.Contains(t.Id) && t.ProjectId == projectId)
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
            float[] leaderAFloats = VectorCalculator.BytesToFloats(leaderA.Embedding);

            foreach (var (clusterId, items) in clusterGroups)
            {
                var members = await db.Reports
                    .AsNoTracking()
                    .Where(t => (t.Id == clusterId || t.ParentReportId == clusterId) && t.ProjectId == projectId)
                    .Select(t => t.Embedding)
                    .Where(e => e != null)
                    .ToListAsync(ct);

                if (members.Count == 0) continue;

                var memberFloats = members
                    .Where(e => e != null)
                    .Select(e => VectorCalculator.BytesToFloats(e!))
                    .ToList();
                var centroid = vectorCalculator.CalculateCentroid(memberFloats);
                var centroidDist = vectorCalculator.CalculateCosineDistance(leaderAFloats, centroid);

                if (bestMatch == null || centroidDist < bestMatch.Distance)
                {
                    bestMatch = new ClusterMatch(clusterId, items.First().Title, centroidDist);
                }
            }

            if (bestMatch != null)
            {
                var targetReport = await db.Reports.AsNoTracking()
                    .Where(t => t.Id == bestMatch.Id && t.ProjectId == projectId)
                    .Select(t => new { t.Id, t.Title, t.Message, t.StackTrace, t.CreatedAt })
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
                            leaderA.Title, leaderA.Message,
                            targetReport.Title, targetReport.Message,
                            ct);

                        if (!areDuplicates)
                        {
                            negativeCache.MarkAsMismatch(tenantId, leaderA.Id, targetReport.Id);
                            goto SuggestionPath;
                        }
                    }

                    await db.Database.ExecuteSqlRawAsync(
                        "UPDATE Reports SET ParentReportId = {0} WHERE ParentReportId = {1} AND ProjectId = {2}",
                        [bestMatch.Id, leaderA.Id, projectId], ct);

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

        await BreakCyclesAsync(db, projectId, tenantId, ct);

        if (mergeCount > 0 || suggestCount > 0)
        {
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Janitor [{TenantId}][Project: {ProjectId}]: Cleanup complete. Merged: {Merge}, Suggested: {Suggest}.",
                tenantId, projectId, mergeCount, suggestCount);
        }
    }

    private async Task BreakCyclesAsync(WinnowDbContext db, Guid projectId, string tenantId, CancellationToken ct)
    {
        // Get all reports with parent references for this specific project
        var candidates = await db.Reports
            .Where(t => t.ProjectId == projectId && t.ParentReportId != null)
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
                    logger.LogWarning("Janitor [{TenantId}][Project: {ProjectId}]: Circular reference detected! Breaking cycle at {Id}",
                        tenantId, projectId, currentId.Value);

                    await db.Database.ExecuteSqlRawAsync(
                        "UPDATE Reports SET ParentReportId = NULL, Status = 'Open' WHERE Id = {0} AND ProjectId = {1}",
                        [currentId.Value, projectId], ct);

                    fixes++;
                    break;
                }

                path.Add(currentId.Value);

                var nextParentId = await db.Reports
                    .Where(t => t.Id == currentId.Value && t.ProjectId == projectId)
                    .Select(t => t.ParentReportId)
                    .FirstOrDefaultAsync(ct);

                if (nextParentId == null) break;

                if (path.Count >= 2)
                {
                    logger.LogInformation("Janitor [{TenantId}][Project: {ProjectId}]: Flattening deep hierarchy {A} -> {B} -> {Root}",
                        tenantId, projectId, path[0], path[1], nextParentId);

                    await db.Database.ExecuteSqlRawAsync(
                        "UPDATE Reports SET ParentReportId = {0} WHERE Id = {1} AND ProjectId = {2}",
                        [nextParentId, path[0], projectId], ct);

                    fixes++;
                    break;
                }

                currentId = nextParentId;
            }
        }

        if (fixes > 0)
        {
            logger.LogInformation("Janitor [{TenantId}][Project: {ProjectId}]: Cycle Breaker fixed {Count} hierarchies.",
                tenantId, projectId, fixes);
        }
    }

    private static async Task<List<ReportMatch>> FindMatchesInDb(WinnowDbContext db, Report target, double threshold, CancellationToken ct)
    {
        var sql = @"
            SELECT t.Id, t.Title, t.Message, t.CreatedAt, v.distance as Distance
            FROM vec_reports v
            JOIN Reports t ON v.rowid = t.rowid
            WHERE v.embedding MATCH {0}
              AND k = 20
              AND v.distance < {1}
              AND t.Id != {2}
              AND t.Status != 'Duplicate'
              AND t.ProjectId = {3}
        ";

        return await db.Database.SqlQueryRaw<ReportMatch>(sql, target.Embedding!, threshold, target.Id, target.ProjectId)
            .ToListAsync(ct);
    }

    private sealed record ReportMatch(Guid Id, string Title, string Message, DateTime CreatedAt, double Distance);
    private sealed record ClusterMatch(Guid Id, string Title, double Distance);
}
