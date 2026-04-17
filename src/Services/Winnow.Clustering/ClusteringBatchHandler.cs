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
    private const double ExactMatchThreshold = 0.15;
    private const double NearMatchThreshold = 0.35;
    private const double SuggestionThreshold = 0.55;

    public async Task Handle(IReadOnlyList<ReportSanitizedEvent> messages, CancellationToken ct)
    {
        logger.LogInformation("Received a batch of {Count} sanitized reports for clustering.", messages.Count);

        var groupsByOrg = messages.GroupBy(m => m.CurrentOrganizationId);

        foreach (var orgGroup in groupsByOrg)
        {
            await ProcessOrganizationBatchAsync(orgGroup.Key, orgGroup.ToList(), ct);
        }
    }

    private async Task ProcessOrganizationBatchAsync(Guid organizationId, List<ReportSanitizedEvent> messages, CancellationToken ct)
    {
        SetTenantContext(organizationId);

        var reportIdsInOrg = messages.Select(m => m.ReportId).ToList();
        var reportsInOrg = await LoadReportsAsync(reportIdsInOrg, ct);
        var groupsByProject = reportsInOrg.GroupBy(r => r.ProjectId);

        foreach (var projectGroup in groupsByProject)
        {
            await ProcessProjectBatchAsync(projectGroup.Key, projectGroup.ToList(), ct);
        }
    }

    private void SetTenantContext(Guid organizationId)
    {
        if (tenantContext is TenantContext concreteContext)
        {
            concreteContext.TenantId = organizationId.ToString();
        }
    }

    private async Task<List<Winnow.API.Domain.Reports.Report>> LoadReportsAsync(List<Guid> reportIds, CancellationToken ct)
    {
        return await dbContext.Reports
            .Where(r => reportIds.Contains(r.Id))
            .ToListAsync(ct);
    }

    private async Task ProcessProjectBatchAsync(Guid projectId, List<Winnow.API.Domain.Reports.Report> reports, CancellationToken ct)
    {
        var modifiedClusterIds = new HashSet<Guid>();
        var projectClusters = await LoadProjectClustersAsync(projectId, ct);

        foreach (var report in reports)
        {
            await AnalyzeAndAssignReportAsync(report, projectClusters, modifiedClusterIds, ct);
        }

        await dbContext.SaveChangesAsync(ct);
        await FinalizeProjectBatchAsync(modifiedClusterIds, ct);
    }

    private async Task<List<Cluster>> LoadProjectClustersAsync(Guid projectId, CancellationToken ct)
    {
        return await dbContext.Clusters
            .Where(c => c.ProjectId == projectId && c.Status != ClusterStatus.Dismissed && c.Centroid != null)
            .ToListAsync(ct);
    }

    private async Task AnalyzeAndAssignReportAsync(
        Winnow.API.Domain.Reports.Report report,
        List<Cluster> projectClusters,
        HashSet<Guid> modifiedClusterIds,
        CancellationToken ct)
    {
        await EnsureEmbeddingAsync(report);

        if (report.Embedding == null || report.Status == ReportStatus.Dismissed) return;

        var bestMatch = FindBestClusterMatch(report, projectClusters);
        if (bestMatch != null)
        {
            await HandleMatchAsync(report, bestMatch, modifiedClusterIds, ct);
        }

        if (report.ClusterId == null && report.SuggestedClusterId == null)
        {
            CreateNewCluster(report, projectClusters, modifiedClusterIds);
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

    private async Task HandleMatchAsync(
        Winnow.API.Domain.Reports.Report report,
        ClusterCandidate bestMatch,
        HashSet<Guid> modifiedClusterIds,
        CancellationToken ct)
    {
        if (bestMatch.Distance <= ExactMatchThreshold)
        {
            ApplyClusterAssignment(report, bestMatch.Cluster, bestMatch.Distance, modifiedClusterIds);
        }
        else if (bestMatch.Distance <= NearMatchThreshold)
        {
            await ResolveNearMatchAsync(report, bestMatch, modifiedClusterIds, ct);
        }
        else if (bestMatch.Distance <= SuggestionThreshold)
        {
            report.SetSuggestedCluster(bestMatch.Cluster.Id, new ConfidenceScore(Math.Max(0, 1.0 - bestMatch.Distance)));
        }
    }

    private async Task ResolveNearMatchAsync(
        Winnow.API.Domain.Reports.Report report,
        ClusterCandidate bestMatch,
        HashSet<Guid> modifiedClusterIds,
        CancellationToken ct)
    {
        var areDuplicates = await CheckIfDuplicateAsync(report, bestMatch.Cluster, ct);

        if (areDuplicates)
        {
            ApplyClusterAssignment(report, bestMatch.Cluster, bestMatch.Distance, modifiedClusterIds);
        }
        else
        {
            report.SetSuggestedCluster(bestMatch.Cluster.Id, new ConfidenceScore(Math.Max(0, 1.0 - bestMatch.Distance)));
        }
    }

    private async Task<bool> CheckIfDuplicateAsync(Winnow.API.Domain.Reports.Report report, Cluster cluster, CancellationToken ct)
    {
        var representative = await dbContext.Reports
            .AsNoTracking()
            .Where(r => r.ClusterId == cluster.Id && r.ProjectId == report.ProjectId)
            .OrderBy(r => r.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (representative == null) return true;

        return await duplicateChecker.AreDuplicatesAsync(
            report.Title, report.Message,
            representative.Title, representative.Message,
            ct);
    }

    private static void ApplyClusterAssignment(Winnow.API.Domain.Reports.Report report, Cluster cluster, double distance, HashSet<Guid> modifiedClusterIds)
    {
        report.AssignToCluster(cluster.Id);
        report.ChangeStatus(ReportStatus.Duplicate);
        report.SetConfidenceScore(new ConfidenceScore(Math.Max(0, 1.0 - distance)));

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

        projectClusters.Add(newCluster);
        modifiedClusterIds.Add(newCluster.Id);
    }

    private async Task FinalizeProjectBatchAsync(HashSet<Guid> modifiedClusterIds, CancellationToken ct)
    {
        foreach (var clusterId in modifiedClusterIds)
        {
            await clusterService.RecalculateCentroidAsync(clusterId, ct);
            await TriggerSummaryIfNecessaryAsync(clusterId, ct);
        }
    }

    private async Task TriggerSummaryIfNecessaryAsync(Guid clusterId, CancellationToken ct)
    {
        var cluster = await dbContext.Clusters.FindAsync([clusterId], ct);
        if (cluster == null || cluster.ReportCount < 5) return;

        var tenMinutesAgo = DateTime.UtcNow.AddMinutes(-10);
        if (cluster.LastSummarizedAt == null || cluster.LastSummarizedAt <= tenMinutesAgo)
        {
            logger.LogInformation("ClusteringBatchConsumer: Cluster {ClusterId} reached mass ({Count}). Triggering summary.",
                cluster.Id, cluster.ReportCount);

            await bus.PublishAsync(new GenerateClusterSummaryEvent(
                cluster.Id, cluster.OrganizationId, cluster.ProjectId));
        }
    }
}

internal sealed record ClusterCandidate(Cluster Cluster, double Distance);
