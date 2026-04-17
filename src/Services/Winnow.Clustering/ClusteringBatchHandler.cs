using Wolverine;
using Winnow.Contracts;
using Microsoft.EntityFrameworkCore;
using Winnow.API.Domain.Clusters;
using Winnow.API.Domain.Clusters.ValueObjects;
using Winnow.API.Domain.Common;
using Winnow.API.Domain.Reports.ValueObjects;
using Winnow.API.Domain.Services;
using Winnow.API.Infrastructure.MultiTenancy;
using Winnow.API.Infrastructure.Persistence;
using Winnow.API.Services.Ai;
using Microsoft.Extensions.Logging;

namespace Winnow.Clustering;

public sealed class ClusteringBatchHandler(
    WinnowDbContext dbContext,
    ILogger<ClusteringBatchHandler> logger,
    ITenantContext tenantContext,
    IDuplicateChecker duplicateChecker,
    IVectorCalculator vectorCalculator,
    IClusterService clusterService,
    IEmbeddingService embeddingService,
    IMessageBus bus)
{
    public async Task Handle(IReadOnlyList<ReportSanitizedEvent> messages, CancellationToken cancellationToken)
    {
        logger.LogInformation("Received a batch of {Count} sanitized reports for clustering.", messages.Count);

        var groupsByOrg = messages.GroupBy(m => m.CurrentOrganizationId);

        foreach (var orgGroup in groupsByOrg)
        {
            await ProcessOrganizationBatchAsync(orgGroup.Key, orgGroup.ToList(), cancellationToken);
        }
    }

    private async Task ProcessOrganizationBatchAsync(Guid organizationId, List<ReportSanitizedEvent> messages, CancellationToken cancellationToken)
    {
        // Set tenant context for this group
        if (tenantContext is TenantContext concreteContext)
        {
            concreteContext.TenantId = organizationId.ToString();
        }

        var reportIdsInOrg = messages.Select(m => m.ReportId).ToList();

        // 1. Load all reports for this organization in this batch
        var reportsInOrg = await dbContext.Reports
            .Where(r => reportIdsInOrg.Contains(r.Id))
            .ToListAsync(cancellationToken);

        // Group by Project within the Organization
        var groupsByProject = reportsInOrg.GroupBy(r => r.ProjectId);

        foreach (var projectGroup in groupsByProject)
        {
            await ProcessProjectBatchAsync(projectGroup.Key, projectGroup.ToList(), cancellationToken);
        }
    }

    private async Task ProcessProjectBatchAsync(Guid projectId, List<Winnow.API.Domain.Reports.Report> reports, CancellationToken cancellationToken)
    {
        var modifiedClusterIds = new HashSet<Guid>();

        // 2. Load all active clusters for this project once
        var projectClusters = await dbContext.Clusters
            .Where(c => c.ProjectId == projectId && c.Status != ClusterStatus.Dismissed && c.Centroid != null)
            .ToListAsync(cancellationToken);

        foreach (var report in reports)
        {
            await ProcessReportAsync(report, projectClusters, modifiedClusterIds, cancellationToken);
        }

        // 6. Save report assignments and new clusters for this project
        await dbContext.SaveChangesAsync(cancellationToken);

        // 7. Recalculate centroids & Trigger Summaries
        await FinalizeBatchAsync(modifiedClusterIds, cancellationToken);
    }

    private async Task ProcessReportAsync(
        Winnow.API.Domain.Reports.Report report,
        List<Cluster> projectClusters,
        HashSet<Guid> modifiedClusterIds,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("ClusteringBatchConsumer: Processing report {Id} (Project: {Project})",
            report.Id, report.ProjectId);

        // 3. Generate embedding if missing
        await EnsureEmbeddingAsync(report);

        if (report.Embedding != null && report.Status != ReportStatus.Dismissed)
        {
            // 4. Cluster Matching
            var bestMatch = FindBestClusterMatch(report, projectClusters);

            if (bestMatch != null)
            {
                await AssignToClusterAsync(report, bestMatch, modifiedClusterIds, cancellationToken);
            }

            if (report.ClusterId == null && report.SuggestedClusterId == null)
            {
                CreateNewCluster(report, projectClusters, modifiedClusterIds);
            }
        }
    }

    private async Task EnsureEmbeddingAsync(Winnow.API.Domain.Reports.Report report)
    {
        if (report.Embedding != null) return;

        var text = $"{report.Title}\n{report.Message}\n{report.StackTrace}";
        var embeddingResult = await embeddingService.GetEmbeddingAsync(text);
        report.SetEmbedding(embeddingResult.Vector);

        if (embeddingResult.Usage != null)
        {
            dbContext.AiUsages.Add(new Winnow.API.Domain.Ai.AiUsage(
                report.OrganizationId,
                "BatchEmbeddingGeneration",
                embeddingResult.Usage.Provider,
                embeddingResult.Usage.ModelId,
                embeddingResult.Usage.PromptTokens,
                embeddingResult.Usage.CompletionTokens
            ));
        }
    }

    private ClusterCandidate? FindBestClusterMatch(Winnow.API.Domain.Reports.Report report, List<Cluster> projectClusters)
    {
        ClusterCandidate? bestMatch = null;

        foreach (var cluster in projectClusters)
        {
            if (cluster.Centroid == null) continue;
            var distance = vectorCalculator.CalculateCosineDistance(report.Embedding!, cluster.Centroid);

            if (bestMatch == null || distance < bestMatch.Distance)
            {
                bestMatch = new ClusterCandidate(cluster, distance);
            }
        }

        return bestMatch;
    }

    private async Task AssignToClusterAsync(
        Winnow.API.Domain.Reports.Report report,
        ClusterCandidate bestMatch,
        HashSet<Guid> modifiedClusterIds,
        CancellationToken cancellationToken)
    {
        if (bestMatch.Distance <= 0.15)
        {
            ApplyClusterAssignment(report, bestMatch.Cluster, bestMatch.Distance, modifiedClusterIds);
        }
        else if (bestMatch.Distance <= 0.35)
        {
            await HandleNearMatchAsync(report, bestMatch, modifiedClusterIds, cancellationToken);
        }
        else if (bestMatch.Distance <= 0.55)
        {
            report.SetSuggestedCluster(bestMatch.Cluster.Id, new ConfidenceScore(Math.Max(0, 1.0 - bestMatch.Distance)));
        }
    }

    private async Task HandleNearMatchAsync(
        Winnow.API.Domain.Reports.Report report,
        ClusterCandidate bestMatch,
        HashSet<Guid> modifiedClusterIds,
        CancellationToken cancellationToken)
    {
        var clusterReports = await dbContext.Reports
            .AsNoTracking()
            .Where(r => r.ClusterId == bestMatch.Cluster.Id && r.ProjectId == report.ProjectId)
            .OrderBy(r => r.CreatedAt)
            .Take(1)
            .ToListAsync(cancellationToken);

        if (clusterReports.Count > 0)
        {
            var representative = clusterReports[0];
            var areDuplicates = await duplicateChecker.AreDuplicatesAsync(
                report.Title, report.Message,
                representative.Title, representative.Message,
                cancellationToken);

            if (areDuplicates)
            {
                ApplyClusterAssignment(report, bestMatch.Cluster, bestMatch.Distance, modifiedClusterIds);
            }
            else
            {
                report.SetSuggestedCluster(bestMatch.Cluster.Id, new ConfidenceScore(Math.Max(0, 1.0 - bestMatch.Distance)));
            }
        }
        else
        {
            ApplyClusterAssignment(report, bestMatch.Cluster, bestMatch.Distance, modifiedClusterIds);
        }
    }

    private static void ApplyClusterAssignment(Winnow.API.Domain.Reports.Report report, Cluster cluster, double distance, HashSet<Guid> modifiedClusterIds)
    {
        report.AssignToCluster(cluster.Id);
        report.ChangeStatus(ReportStatus.Duplicate);
        report.SetConfidenceScore(new ConfidenceScore(Math.Max(0, 1.0 - distance)));

        // Maintain domain bidirectional relationship
        cluster.AddReport(report.Id);
        modifiedClusterIds.Add(cluster.Id);
    }

    private void CreateNewCluster(Winnow.API.Domain.Reports.Report report, List<Cluster> projectClusters, HashSet<Guid> modifiedClusterIds)
    {
        logger.LogInformation("ClusteringBatchConsumer: No cluster match for report {Id}. Creating new cluster.", report.Id);

        var newCluster = new Cluster(report.ProjectId, report.OrganizationId, report.Id);
        newCluster.UpdateCentroid(report.Embedding!);

        dbContext.Clusters.Add(newCluster);
        report.AssignToCluster(newCluster.Id);

        // Important: Add to projectClusters so other reports in THIS batch can join it!
        projectClusters.Add(newCluster);
        modifiedClusterIds.Add(newCluster.Id);
    }

    private async Task FinalizeBatchAsync(HashSet<Guid> modifiedClusterIds, CancellationToken cancellationToken)
    {
        foreach (var clusterId in modifiedClusterIds)
        {
            await clusterService.RecalculateCentroidAsync(clusterId, cancellationToken);
            await CheckForSummaryTriggerAsync(clusterId, cancellationToken);
        }
    }

    private async Task CheckForSummaryTriggerAsync(Guid clusterId, CancellationToken cancellationToken)
    {
        var cluster = await dbContext.Clusters.FindAsync([clusterId], cancellationToken);
        if (cluster == null || cluster.ReportCount < 5) return;

        var tenMinutesAgo = DateTime.UtcNow.AddMinutes(-10);
        if (cluster.LastSummarizedAt == null || cluster.LastSummarizedAt <= tenMinutesAgo)
        {
            logger.LogInformation("ClusteringBatchConsumer: Cluster {ClusterId} reached critical mass ({Count} reports). Requesting summary.",
                cluster.Id, cluster.ReportCount);

            await bus.PublishAsync(new GenerateClusterSummaryEvent(
                cluster.Id,
                cluster.OrganizationId,
                cluster.ProjectId));
        }
    }
}

internal sealed record ClusterCandidate(Cluster Cluster, double Distance);
