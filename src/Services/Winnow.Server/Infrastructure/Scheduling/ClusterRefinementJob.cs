using Microsoft.EntityFrameworkCore;
using Winnow.Server.Domain.Services;
using Winnow.Server.Entities;
using Winnow.Server.Infrastructure.Persistence;
using Winnow.Server.Services.Ai;

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
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<WinnowDbContext>();

                logger.LogInformation("Janitor: Starting global cluster refinement cycle.");

                var projects = await db.Projects
                    .AsNoTracking()
                    .Select(p => p.Id)
                    .ToListAsync(stoppingToken);

                var embeddingService = scope.ServiceProvider.GetRequiredService<IEmbeddingService>();
                var duplicateChecker = scope.ServiceProvider.GetRequiredService<IDuplicateChecker>();
                var negativeCache = scope.ServiceProvider.GetRequiredService<INegativeMatchCache>();
                var vectorCalculator = scope.ServiceProvider.GetRequiredService<IVectorCalculator>();
                var clusterService = scope.ServiceProvider.GetRequiredService<IClusterService>();

                foreach (var projectId in projects)
                {
                    try
                    {
                        await ProcessProjectAsync(db, projectId, embeddingService, duplicateChecker, negativeCache, vectorCalculator, clusterService, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Janitor: Refinement failed for Project {ProjectId}", projectId);
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

    private async Task ProcessProjectAsync(
        WinnowDbContext db,
        Guid projectId,
        IEmbeddingService embeddingService,
        IDuplicateChecker duplicateChecker,
        INegativeMatchCache negativeCache,
        IVectorCalculator vectorCalculator,
        IClusterService clusterService,
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
                logger.LogInformation("Janitor [Project: {ProjectId}]: Generating missing embedding for report {Id}",
                    projectId, report.Id);
                var text = $"{report.Title}\n{report.Message}\n{report.StackTrace}";
                var embeddingFloats = await embeddingService.GetEmbeddingAsync(text);

                // Use direct SQL for the update to ensure it's saved even if other changes fail
                await db.Database.ExecuteSqlRawAsync(
                    "UPDATE \"Reports\" SET \"Embedding\" = {0} WHERE \"Id\" = {1} AND \"ProjectId\" = {2}",
                    [embeddingFloats, report.Id, projectId], ct);

                report.Embedding = embeddingFloats;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Janitor [Project: {ProjectId}]: Failed to heal embedding for {Id}",
                    projectId, report.Id);
            }
        }

        // 2. Load all open clusters for this project
        var clusters = await db.Clusters
            .Where(c => c.ProjectId == projectId && c.Status != "Closed" && c.Centroid != null)
            .ToListAsync(ct);

        // 3. Match orphan reports against existing clusters
        int reportMergeCount = 0;
        int reportSuggestCount = 0;

        const double ReportHardMergeThreshold = 0.35;
        const double ReportSuggestThreshold = 0.55;

        foreach (var report in orphanReports)
        {
            if (report.Embedding == null) continue;

            ClusterCandidate? bestMatch = null;

            foreach (var cluster in clusters)
            {
                if (cluster.Centroid == null) continue;

                if (negativeCache.IsKnownMismatch("default", report.Id, cluster.Id))
                    continue;

                var distance = vectorCalculator.CalculateCosineDistance(report.Embedding, cluster.Centroid);

                if (bestMatch == null || distance < bestMatch.Distance)
                {
                    bestMatch = new ClusterCandidate(cluster.Id, distance);
                }
            }

            if (bestMatch == null) continue;

            if (bestMatch.Distance <= ReportHardMergeThreshold)
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
                        if (negativeCache.IsKnownMismatch("default", report.Id, representative.Id))
                            continue;

                        var areDuplicates = await duplicateChecker.AreDuplicatesAsync(
                            report.Title, report.Message,
                            representative.Title, representative.Message,
                            ct);

                        if (!areDuplicates)
                        {
                            negativeCache.MarkAsMismatch("default", report.Id, bestMatch.Id);
                            goto ReportSuggestionPath;
                        }
                    }
                }

                report.ClusterId = bestMatch.Id;
                report.Status = "Duplicate";
                report.ConfidenceScore = (float)Math.Max(0, 1.0 - bestMatch.Distance);
                report.SuggestedClusterId = null;
                report.SuggestedConfidenceScore = null;
                reportMergeCount++;
                continue;
            }

        ReportSuggestionPath:
            if (bestMatch.Distance <= ReportSuggestThreshold)
            {
                if (report.SuggestedClusterId == null)
                {
                    report.SuggestedClusterId = bestMatch.Id;
                    report.SuggestedConfidenceScore = (float)Math.Max(0, 1.0 - bestMatch.Distance);
                    reportSuggestCount++;
                }
            }
        }

        // 4. Create clusters for remaining orphans
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
            clusters.Add(newCluster);
        }

        // 5. Cluster-to-Cluster Merging
        int clusterMergeCount = 0;
        int clusterSuggestCount = 0;

        const double ClusterHardMergeThreshold = 0.25; // More strict than reports
        const double ClusterSuggestThreshold = 0.45;

        for (int i = 0; i < clusters.Count; i++)
        {
            var c1 = clusters[i];
            if (c1.Centroid == null) continue;

            ClusterCandidate? bestClusterMatch = null;

            for (int j = 0; j < clusters.Count; j++)
            {
                if (i == j) continue;
                var c2 = clusters[j];
                if (c2.Centroid == null) continue;

                // Skip if c2 is already suggested to merge into something else or a duplicate
                // (though clusters don't have a Dupe status yet, they might be suggested)

                var distance = vectorCalculator.CalculateCosineDistance(c1.Centroid, c2.Centroid);

                if (bestClusterMatch == null || distance < bestClusterMatch.Distance)
                {
                    bestClusterMatch = new ClusterCandidate(c2.Id, distance);
                }
            }

            if (bestClusterMatch != null)
            {
                if (bestClusterMatch.Distance <= ClusterHardMergeThreshold)
                {
                    // High confidence: Auto-merge
                    // In a real scenario, we'd move all reports from c1 to c2 and delete c1.
                    // For now, let's follow the user's "same behavior as reports" - although for clusters, 
                    // "merging" usually means combining them.

                    // TODO: Implement actual data migration for auto-merge.
                    // For now, we'll suggest it with 100% confidence to let the user confirm, 
                    // unless we want to be truly proactive.

                    // User said: "auto merge ones we're EXTREMELY confident in and suggest ones that LOOK similar"

                    await PerformClusterMergeAsync(db, c1.Id, bestClusterMatch.Id, ct);
                    clusterMergeCount++;

                    // Remove c1 from our local list so we don't process it further
                    clusters.RemoveAt(i);
                    i--;
                    continue;
                }
                else if (bestClusterMatch.Distance <= ClusterSuggestThreshold)
                {
                    // Suggest merge
                    c1.SuggestedMergeClusterId = bestClusterMatch.Id;
                    c1.SuggestedMergeConfidenceScore = (float)Math.Max(0, 1.0 - bestClusterMatch.Distance);
                    clusterSuggestCount++;
                }
                else
                {
                    // Clear previous suggestions if they no longer match
                    c1.SuggestedMergeClusterId = null;
                    c1.SuggestedMergeConfidenceScore = null;
                }
            }
        }

        // 6. Recalculate centroids
        foreach (var cluster in clusters)
        {
            await clusterService.RecalculateCentroidAsync(cluster.Id, ct);
        }

        if (reportMergeCount > 0 || reportSuggestCount > 0 || clusterMergeCount > 0 || clusterSuggestCount > 0)
        {
            logger.LogInformation("Janitor [Project: {ProjectId}]: Cycle complete.\n" +
                "  Reports: Merged {RMerge}, Suggested {RSuggest}\n" +
                "  Clusters: Merged {CMerge}, Suggested {CSuggest}",
                projectId, reportMergeCount, reportSuggestCount, clusterMergeCount, clusterSuggestCount);
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task PerformClusterMergeAsync(WinnowDbContext db, Guid sourceClusterId, Guid targetClusterId, CancellationToken ct)
    {
        logger.LogInformation("Janitor: Auto-merging cluster {Source} into {Target}", sourceClusterId, targetClusterId);

        // Move all reports and mark as Duplicate
        await db.Reports
            .Where(r => r.ClusterId == sourceClusterId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.ClusterId, targetClusterId)
                .SetProperty(r => r.Status, "Duplicate"), ct);

        // Delete source cluster
        await db.Clusters
            .Where(c => c.Id == sourceClusterId)
            .ExecuteDeleteAsync(ct);
    }

    private sealed record ClusterCandidate(Guid Id, double Distance);
}
