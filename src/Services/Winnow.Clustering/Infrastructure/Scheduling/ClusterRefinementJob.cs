using System.Diagnostics.CodeAnalysis;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Winnow.API.Domain.Clusters;
using Winnow.API.Domain.Clusters.ValueObjects;
using Winnow.API.Domain.Common;
using Winnow.API.Domain.Reports;
using Winnow.API.Domain.Reports.ValueObjects;
using Winnow.API.Domain.Services;
using Winnow.API.Infrastructure.Persistence;
using Winnow.API.Services.Ai;
using Winnow.Contracts;

namespace Winnow.Clustering.Infrastructure.Scheduling;

internal sealed class ClusterRefinementJob(
    IServiceScopeFactory scopeFactory,
    IPublishEndpoint publishEndpoint,
    ILogger<ClusterRefinementJob> logger) : BackgroundService
{
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Background service loop must continue on failure")]
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
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Janitor: Refinement failed for Project {ProjectId}", projectId);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
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

        // 4. Trigger Summarization for clusters that reached critical mass during this cycle
        var tenMinutesAgo = DateTime.UtcNow.AddMinutes(-10);
        foreach (var cluster in clusters)
        {
            if (cluster.ReportCount >= 5 && (cluster.LastSummarizedAt == null || cluster.LastSummarizedAt <= tenMinutesAgo))
            {
                await publishEndpoint.Publish(new GenerateClusterSummaryEvent(
                    cluster.Id,
                    cluster.OrganizationId,
                    cluster.ProjectId), ct);
            }
        }
    }

    private static async Task<List<Report>> GetOrphanReportsAsync(WinnowDbContext db, Guid projectId, CancellationToken ct)
    {
        // We only want reports that are truly "orphan" (no cluster) and "open"
        return await db.Reports
            .Where(t => t.ProjectId == projectId
                     && t.ClusterId == null
                     && t.Status == ReportStatus.Open)
            .OrderBy(t => t.CreatedAt)
            .ToListAsync(ct);
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Background service loop must continue on failure")]
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
                var embeddingResult = await embeddingService.GetEmbeddingAsync(text);
                var embeddingFloats = embeddingResult.Vector;

                // Log auditing info
                if (embeddingResult.Usage != null)
                {
                    db.AiUsages.Add(new Winnow.API.Domain.Ai.AiUsage(
                        report.OrganizationId,
                        "JanitorEmbeddingGeneration",
                        embeddingResult.Usage.Provider,
                        embeddingResult.Usage.ModelId,
                        embeddingResult.Usage.PromptTokens,
                        embeddingResult.Usage.CompletionTokens
                    ));
                }

                // Update using EF tracking instead of raw SQL to ensure converters are used
                report.SetEmbedding(embeddingFloats);
                await db.SaveChangesAsync(ct);
            }
            catch (OperationCanceledException)
            {
                throw;
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
        return await db.Clusters
            .Where(c => c.ProjectId == projectId && c.Status != ClusterStatus.Dismissed && c.Status != ClusterStatus.Merged && c.Centroid != null)
            .ToListAsync(ct);
    }

    private static async Task<(int ReportMergeCount, int ReportSuggestCount)> MatchOrphanReportsAsync(
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
            if (report.Embedding == null || report.ClusterId != null || report.SuggestedClusterId != null) continue;

            ClusterCandidate? bestMatch = FindBestClusterMatch(report, clusters, negativeCache, vectorCalculator);

            if (bestMatch == null || bestMatch.Distance > ReportSuggestThreshold)
            {
                // Create a new cluster for this completely unmatched report immediately
                // so subsequent orphans in this batch can match against it.
                var newCluster = new Cluster(projectId, report.OrganizationId, report.Id);
                newCluster.UpdateCentroid(report.Embedding!);
                db.Clusters.Add(newCluster);
                report.AssignToCluster(newCluster.Id);
                clusters.Add(newCluster);
                continue;
            }

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
        report.ChangeStatus(ReportStatus.Duplicate);
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

    private static async Task<bool> VerifyDuplicateAsync(
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
        var statusDuplicate = ReportStatus.Duplicate;
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
