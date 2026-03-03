using Microsoft.EntityFrameworkCore;
using Winnow.Server.Domain.Services;
using Winnow.Server.Entities;
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

        if (db.Database.IsSqlite())
        {
            await db.Database.ExecuteSqlRawAsync(
                "CREATE VIRTUAL TABLE IF NOT EXISTS vec_reports USING vec0(embedding float[384] distance_metric=cosine);",
                ct);
        }

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
        // 1. Heal missing embeddings on orphan reports
        var orphanReports = await db.Reports
            .Where(t => t.ProjectId == projectId
                     && t.ClusterId == null
                     && t.Status != "Duplicate"
                     && t.Status != "Closed")
            .OrderBy(t => t.CreatedAt)
            .ToListAsync(ct);

        foreach (var report in orphanReports.Where(r => r.Embedding == null))
        {
            try
            {
                logger.LogInformation("Janitor [{TenantId}][Project: {ProjectId}]: Generating missing embedding for report {Id}",
                    tenantId, projectId, report.Id);
                var text = $"{report.Title}\n{report.Message}\n{report.StackTrace}";
                var embeddingFloats = await embeddingService.GetEmbeddingAsync(text);

                await db.Database.ExecuteSqlRawAsync(
                    "UPDATE Reports SET Embedding = {0} WHERE Id = {1} AND ProjectId = {2}",
                    [embeddingFloats, report.Id, projectId], ct);

                if (db.Database.IsSqlite())
                {
                    await db.Database.ExecuteSqlRawAsync(
                        "DELETE FROM vec_reports WHERE rowid = (SELECT rowid FROM Reports WHERE Id = {0})",
                        [report.Id], ct);

                    await db.Database.ExecuteSqlRawAsync(
                        "INSERT INTO vec_reports(rowid, embedding) VALUES ((SELECT rowid FROM Reports WHERE Id = {0} AND ProjectId = {1}), {2})",
                        [report.Id, projectId, embeddingFloats], ct);
                }

                report.Embedding = embeddingFloats;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Janitor [{TenantId}][Project: {ProjectId}]: Failed to heal embedding for {Id}",
                    tenantId, projectId, report.Id);
            }
        }

        // 2. Load all clusters for this project
        var clusters = await db.Clusters
            .Where(c => c.ProjectId == projectId && c.Status != "Closed" && c.Centroid != null)
            .ToListAsync(ct);

        // 3. Try to match orphan reports against existing clusters
        int mergeCount = 0;
        int suggestCount = 0;

        const double HardMergeThreshold = 0.35;
        const double SuggestThreshold = 0.55;

        foreach (var report in orphanReports)
        {
            if (report.Embedding == null) continue;

            ClusterCandidate? bestMatch = null;

            foreach (var cluster in clusters)
            {
                if (cluster.Centroid == null) continue;

                if (negativeCache.IsKnownMismatch(tenantId, report.Id, cluster.Id))
                    continue;

                var distance = vectorCalculator.CalculateCosineDistance(report.Embedding, cluster.Centroid);

                if (bestMatch == null || distance < bestMatch.Distance)
                {
                    bestMatch = new ClusterCandidate(cluster.Id, distance);
                }
            }

            if (bestMatch == null) continue;

            if (bestMatch.Distance <= HardMergeThreshold)
            {
                if (bestMatch.Distance > 0.15)
                {
                    // AI-confirm for 0.15–0.35 range
                    var representative = await db.Reports
                        .AsNoTracking()
                        .Where(r => r.ClusterId == bestMatch.Id && r.ProjectId == projectId)
                        .OrderBy(r => r.CreatedAt)
                        .FirstOrDefaultAsync(ct);

                    if (representative != null)
                    {
                        if (negativeCache.IsKnownMismatch(tenantId, report.Id, representative.Id))
                            continue;

                        var areDuplicates = await duplicateChecker.AreDuplicatesAsync(
                            report.Title, report.Message,
                            representative.Title, representative.Message,
                            ct);

                        if (!areDuplicates)
                        {
                            negativeCache.MarkAsMismatch(tenantId, report.Id, bestMatch.Id);
                            goto SuggestionPath;
                        }
                    }
                }

                report.ClusterId = bestMatch.Id;
                report.Status = "Duplicate";
                report.ConfidenceScore = (float)Math.Max(0, 1.0 - bestMatch.Distance);
                report.SuggestedClusterId = null;
                report.SuggestedConfidenceScore = null;
                mergeCount++;
                continue;
            }

        SuggestionPath:
            if (bestMatch.Distance <= SuggestThreshold)
            {
                if (report.SuggestedClusterId == null)
                {
                    report.SuggestedClusterId = bestMatch.Id;
                    report.SuggestedConfidenceScore = (float)Math.Max(0, 1.0 - bestMatch.Distance);
                    suggestCount++;
                }
            }
        }

        // 4. Create clusters for remaining orphans that have embeddings but no cluster
        foreach (var report in orphanReports.Where(r => r.Embedding != null && r.ClusterId == null && r.SuggestedClusterId == null))
        {
            var newCluster = new Cluster
            {
                ProjectId = projectId,
                OrganizationId = report.OrganizationId,
                Centroid = report.Embedding,
                Title = report.Title,
                Status = "Open",
            };
            db.Clusters.Add(newCluster);
            report.ClusterId = newCluster.Id;
            clusters.Add(newCluster); // Track for centroid recalc
        }

        // 5. Recalculate centroids for all clusters that had reports added
        foreach (var cluster in clusters)
        {
            var memberEmbeddings = await db.Reports
                .AsNoTracking()
                .Where(r => r.ClusterId == cluster.Id && r.Embedding != null)
                .Select(r => r.Embedding!)
                .ToListAsync(ct);

            if (memberEmbeddings.Count > 0)
            {
                cluster.Centroid = vectorCalculator.CalculateCentroid(memberEmbeddings);
            }
        }

        if (mergeCount > 0 || suggestCount > 0)
        {
            logger.LogInformation("Janitor [{TenantId}][Project: {ProjectId}]: Cleanup complete. Merged: {Merge}, Suggested: {Suggest}.",
                tenantId, projectId, mergeCount, suggestCount);
        }

        await db.SaveChangesAsync(ct);
    }

    private sealed record ClusterCandidate(Guid Id, double Distance);
}
