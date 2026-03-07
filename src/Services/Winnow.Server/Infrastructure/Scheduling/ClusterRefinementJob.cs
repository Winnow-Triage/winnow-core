using Microsoft.EntityFrameworkCore;
using Winnow.Server.Domain.Clusters;
using Winnow.Server.Domain.Clusters.ValueObjects;
using Winnow.Server.Domain.Common;
using Winnow.Server.Domain.Reports;
using Winnow.Server.Domain.Reports.ValueObjects;
using Winnow.Server.Domain.Services;
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
                logger.LogInformation("Janitor: Starting global cluster refinement cycle.");

                List<Guid> projects;
                using (var outerScope = scopeFactory.CreateScope())
                {
                    var db = outerScope.ServiceProvider.GetRequiredService<WinnowDbContext>();
                    projects = await db.Projects
                        .AsNoTracking()
                        .Select(p => p.Id)
                        .ToListAsync(stoppingToken);
                }

                foreach (var projectId in projects)
                {
                    try
                    {
                        using var scope = scopeFactory.CreateScope();
                        var db = scope.ServiceProvider.GetRequiredService<WinnowDbContext>();
                        var embeddingService = scope.ServiceProvider.GetRequiredService<IEmbeddingService>();
                        var duplicateChecker = scope.ServiceProvider.GetRequiredService<IDuplicateChecker>();
                        var negativeCache = scope.ServiceProvider.GetRequiredService<INegativeMatchCache>();
                        var vectorCalculator = scope.ServiceProvider.GetRequiredService<IVectorCalculator>();
                        var clusterService = scope.ServiceProvider.GetRequiredService<IClusterService>();

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

    internal async Task ProcessProjectAsync(
        WinnowDbContext db,
        Guid projectId,
        IEmbeddingService embeddingService,
        IDuplicateChecker duplicateChecker,
        INegativeMatchCache negativeCache,
        IVectorCalculator vectorCalculator,
        IClusterService clusterService,
        CancellationToken ct)
    {
        var orphanReports = await GetOrphanReportsAsync(db, projectId, ct);

        await HealMissingEmbeddingsAsync(db, projectId, orphanReports, embeddingService, ct);

        // Load all open clusters for this project
        var clusters = await GetOpenClustersAsync(db, projectId, ct);

        var (reportMergeCount, reportSuggestCount) = await MatchOrphanReportsAsync(
            db, projectId, orphanReports, clusters, duplicateChecker, negativeCache, vectorCalculator, ct);

        CreateClustersForRemainingOrphans(db, projectId, orphanReports, clusters);

        // Save changes before cluster-to-cluster merging so we don't have tracked entities
        // depending on clusters that might be deleted via ExecuteDeleteAsync.
        await db.SaveChangesAsync(ct);

        var (clusterMergeCount, clusterSuggestCount) = await MergeClustersAsync(db, clusters, vectorCalculator, ct);

        await RecalculateCentroidsAsync(clusters, clusterService, ct);

        if (reportMergeCount > 0 || reportSuggestCount > 0 || clusterMergeCount > 0 || clusterSuggestCount > 0)
        {
            logger.LogInformation("Janitor [Project: {ProjectId}]: Cycle complete.\n" +
                "  Reports: Merged {RMerge}, Suggested {RSuggest}\n" +
                "  Clusters: Merged {CMerge}, Suggested {CSuggest}",
                projectId, reportMergeCount, reportSuggestCount, clusterMergeCount, clusterSuggestCount);
        }

        await db.SaveChangesAsync(ct);
    }

    private static async Task<List<Report>> GetOrphanReportsAsync(WinnowDbContext db, Guid projectId, CancellationToken ct)
    {
        var statusDuplicate = ReportStatus.Dismissed; // Mapped to duplicate for now 
        var statusClosed = ReportStatus.Dismissed;    // Same mappings used elsewhere

        return await db.Reports
            .Where(t => t.ProjectId == projectId
                     && t.ClusterId == null
                     && t.Status != statusDuplicate
                     && t.Status != statusClosed)
            .OrderBy(t => t.CreatedAt)
            .ToListAsync(ct);
    }

    private async Task HealMissingEmbeddingsAsync(
        WinnowDbContext db,
        Guid projectId,
        List<Report> orphanReports,
        IEmbeddingService embeddingService,
        CancellationToken ct)
    {
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

                report.SetEmbedding(embeddingFloats);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Janitor [Project: {ProjectId}]: Failed to heal embedding for {Id}",
                    projectId, report.Id);
            }
        }
    }

    private static async Task<List<Cluster>> GetOpenClustersAsync(WinnowDbContext db, Guid projectId, CancellationToken ct)
    {
        var statusClosed = ClusterStatus.Dismissed;
        return await db.Clusters
            .Where(c => c.ProjectId == projectId && c.Status != statusClosed && c.Centroid != null)
            .ToListAsync(ct);
    }

    private async Task<(int ReportMergeCount, int ReportSuggestCount)> MatchOrphanReportsAsync(
        WinnowDbContext db,
        Guid projectId,
        List<Report> orphanReports,
        List<Cluster> clusters,
        IDuplicateChecker duplicateChecker,
        INegativeMatchCache negativeCache,
        IVectorCalculator vectorCalculator,
        CancellationToken ct)
    {
        int reportMergeCount = 0;
        int reportSuggestCount = 0;

        const double ReportHardMergeThreshold = 0.35;
        const double ReportSuggestThreshold = 0.55;

        foreach (var report in orphanReports)
        {
            if (report.Embedding == null) continue;

            ClusterCandidate? bestMatch = FindBestClusterMatch(report, clusters, negativeCache, vectorCalculator);

            if (bestMatch == null) continue;

            if (bestMatch.Distance <= ReportHardMergeThreshold)
            {
                if (bestMatch.Distance > 0.15)
                {
                    bool isDuplicate = await VerifyDuplicateAsync(db, projectId, report, bestMatch, duplicateChecker, negativeCache, ct);
                    if (!isDuplicate)
                    {
                        if (bestMatch.Distance <= ReportSuggestThreshold && report.SuggestedClusterId == null)
                        {
                            report.SetSuggestedCluster(bestMatch.Id, new ConfidenceScore(1.0 - bestMatch.Distance));
                            reportSuggestCount++;
                        }
                        continue;
                    }
                }

                MergeReportIntoCluster(report, bestMatch);
                reportMergeCount++;
                continue;
            }

            if (bestMatch.Distance <= ReportSuggestThreshold && report.SuggestedClusterId == null)
            {
                report.SetSuggestedCluster(bestMatch.Id, new ConfidenceScore(1.0 - bestMatch.Distance));
                reportSuggestCount++;
            }
        }

        return (reportMergeCount, reportSuggestCount);
    }

    private static void MergeReportIntoCluster(Report report, ClusterCandidate bestMatch)
    {
        report.AssignToCluster(bestMatch.Id);
        report.ChangeStatus(ReportStatus.Dismissed);
        report.SetConfidenceScore(new ConfidenceScore(Math.Max(0, 1.0 - bestMatch.Distance)));
        // Note: AssignToCluster clears suggestions inherently in the domain model
    }

    private static ClusterCandidate? FindBestClusterMatch(Report report, List<Cluster> clusters, INegativeMatchCache negativeCache, IVectorCalculator vectorCalculator)
    {
        ClusterCandidate? bestMatch = null;

        foreach (var cluster in clusters)
        {
            if (cluster.Centroid == null) continue;

            if (negativeCache.IsKnownMismatch("default", report.Id, cluster.Id))
                continue;

            var distance = vectorCalculator.CalculateCosineDistance(report.Embedding!, cluster.Centroid);

            if (bestMatch == null || distance < bestMatch.Distance)
            {
                bestMatch = new ClusterCandidate(cluster.Id, distance);
            }
        }

        return bestMatch;
    }

    private async Task<bool> VerifyDuplicateAsync(
        WinnowDbContext db,
        Guid projectId,
        Report report,
        ClusterCandidate bestMatch,
        IDuplicateChecker duplicateChecker,
        INegativeMatchCache negativeCache,
        CancellationToken ct)
    {
        var representative = await db.Reports
            .AsNoTracking()
            .Where(r => r.ClusterId == bestMatch.Id && r.ProjectId == projectId)
            .OrderBy(r => r.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (representative != null)
        {
            if (negativeCache.IsKnownMismatch("default", report.Id, representative.Id))
                return false;

            var areDuplicates = await duplicateChecker.AreDuplicatesAsync(
                report.Title, report.Message,
                representative.Title, representative.Message,
                ct);

            if (!areDuplicates)
            {
                negativeCache.MarkAsMismatch("default", report.Id, bestMatch.Id);
                return false;
            }
        }
        return true;
    }

    private static void CreateClustersForRemainingOrphans(WinnowDbContext db, Guid projectId, List<Report> orphanReports, List<Cluster> clusters)
    {
        foreach (var report in orphanReports.Where(r => r.Embedding != null && r.ClusterId == null && r.SuggestedClusterId == null))
        {
            var newCluster = new Cluster(projectId, report.OrganizationId, report.Id);
            newCluster.UpdateCentroid(report.Embedding!);
            // newCluster.SetSummary(report.Title, null); // We don't have a reliable way to set title only via domain methods if summary is missing, but maybe it's fine. Wait, does Cluster constructor set Status = Open? Yes.
            db.Clusters.Add(newCluster);
            report.AssignToCluster(newCluster.Id);
            clusters.Add(newCluster);
        }
    }

    private async Task<(int ClusterMergeCount, int ClusterSuggestCount)> MergeClustersAsync(WinnowDbContext db, List<Cluster> clusters, IVectorCalculator vectorCalculator, CancellationToken ct)
    {
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
                    await PerformClusterMergeAsync(db, c1.Id, bestClusterMatch.Id, ct);
                    clusterMergeCount++;

                    clusters.RemoveAt(i);
                    i--;
                    continue;
                }
                else if (bestClusterMatch.Distance <= ClusterSuggestThreshold)
                {
                    c1.SuggestMerge(bestClusterMatch.Id, new ConfidenceScore(Math.Max(0, 1.0 - bestClusterMatch.Distance)));
                    clusterSuggestCount++;
                }
                else
                {
                    c1.ClearMergeSuggestion();
                }
            }
        }

        return (clusterMergeCount, clusterSuggestCount);
    }

    private static async Task RecalculateCentroidsAsync(List<Cluster> clusters, IClusterService clusterService, CancellationToken ct)
    {
        foreach (var cluster in clusters)
        {
            await clusterService.RecalculateCentroidAsync(cluster.Id, ct);
        }
    }

    private async Task PerformClusterMergeAsync(WinnowDbContext db, Guid sourceClusterId, Guid targetClusterId, CancellationToken ct)
    {
        logger.LogInformation("Janitor: Auto-merging cluster {Source} into {Target}", sourceClusterId, targetClusterId);

        // Move all reports and mark as Duplicate
        var statusDuplicate = ReportStatus.Dismissed;
        await db.Reports
            .Where(r => r.ClusterId == sourceClusterId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.ClusterId, targetClusterId)
                .SetProperty(r => r.Status, statusDuplicate), ct);

        // Clear any suggestion references pointing to the source cluster before deleting it
        await db.Reports
            .Where(r => r.SuggestedClusterId == sourceClusterId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.SuggestedClusterId, (Guid?)null)
                .SetProperty(r => r.SuggestedConfidenceScore, (ConfidenceScore?)null), ct);

        await db.Clusters
            .Where(c => c.SuggestedMergeClusterId == sourceClusterId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.SuggestedMergeClusterId, (Guid?)null)
                .SetProperty(c => c.SuggestedMergeConfidenceScore, (ConfidenceScore?)null), ct);

        // Delete source cluster
        await db.Clusters
            .Where(c => c.Id == sourceClusterId)
            .ExecuteDeleteAsync(ct);
    }

    private sealed record ClusterCandidate(Guid Id, double Distance);
}
