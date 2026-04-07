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

        // Group by Organization to respect multi-tenancy filters
        var groupsByOrg = messages.GroupBy(m => m.CurrentOrganizationId);

        foreach (var orgGroup in groupsByOrg)
        {
            var organizationId = orgGroup.Key;

            // Set tenant context for this group
            if (tenantContext is TenantContext concreteContext)
            {
                concreteContext.TenantId = organizationId.ToString();
            }

            var reportIdsInOrg = orgGroup.Select(m => m.ReportId).ToList();

            // 1. Load all reports for this organization in this batch
            var reportsInOrg = await dbContext.Reports
                .Where(r => reportIdsInOrg.Contains(r.Id))
                .ToListAsync(cancellationToken);

            // Group by Project within the Organization
            var groupsByProject = reportsInOrg.GroupBy(r => r.ProjectId);

            foreach (var projectGroup in groupsByProject)
            {
                var projectId = projectGroup.Key;
                var modifiedClusterIds = new HashSet<Guid>();

                // 2. Load all active clusters for this project once
                var projectClusters = await dbContext.Clusters
                    .Where(c => c.ProjectId == projectId && c.Status != ClusterStatus.Dismissed && c.Centroid != null)
                    .ToListAsync(cancellationToken);

                foreach (var report in projectGroup)
                {
                    logger.LogInformation("ClusteringBatchConsumer: Processing report {Id} (Project: {Project})",
                        report.Id, report.ProjectId);

                    // 3. Generate embedding if missing
                    if (report.Embedding == null)
                    {
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

                    if (report.Embedding != null && report.Status != ReportStatus.Dismissed)
                    {
                        // 4. Cluster Matching
                        ClusterCandidate? bestMatch = null;

                        foreach (var cluster in projectClusters)
                        {
                            if (cluster.Centroid == null) continue;
                            var distance = vectorCalculator.CalculateCosineDistance(report.Embedding, cluster.Centroid);

                            if (bestMatch == null || distance < bestMatch.Distance)
                            {
                                bestMatch = new ClusterCandidate(cluster, distance);
                            }
                        }

                        if (bestMatch != null)
                        {
                            if (bestMatch.Distance <= 0.15)
                            {
                                report.AssignToCluster(bestMatch.Cluster.Id);
                                report.ChangeStatus(ReportStatus.Duplicate);
                                report.SetConfidenceScore(new ConfidenceScore(Math.Max(0, 1.0 - bestMatch.Distance)));

                                // Maintain domain bidirectional relationship
                                bestMatch.Cluster.AddReport(report.Id);
                                modifiedClusterIds.Add(bestMatch.Cluster.Id);
                            }
                            else if (bestMatch.Distance <= 0.35)
                            {
                                var clusterReports = await dbContext.Reports
                                    .AsNoTracking()
                                    .Where(r => r.ClusterId == bestMatch.Cluster.Id && r.ProjectId == projectId)
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
                                        report.AssignToCluster(bestMatch.Cluster.Id);
                                        report.ChangeStatus(ReportStatus.Duplicate);
                                        report.SetConfidenceScore(new ConfidenceScore(Math.Max(0, 1.0 - bestMatch.Distance)));

                                        // Maintain domain bidirectional relationship
                                        bestMatch.Cluster.AddReport(report.Id);
                                        modifiedClusterIds.Add(bestMatch.Cluster.Id);
                                    }
                                    else
                                    {
                                        report.SetSuggestedCluster(bestMatch.Cluster.Id, new ConfidenceScore(Math.Max(0, 1.0 - bestMatch.Distance)));
                                    }
                                }
                                else
                                {
                                    report.AssignToCluster(bestMatch.Cluster.Id);
                                    report.ChangeStatus(ReportStatus.Duplicate);
                                    report.SetConfidenceScore(new ConfidenceScore(Math.Max(0, 1.0 - bestMatch.Distance)));

                                    // Maintain domain bidirectional relationship
                                    bestMatch.Cluster.AddReport(report.Id);
                                    modifiedClusterIds.Add(bestMatch.Cluster.Id);
                                }
                            }
                            else if (bestMatch.Distance <= 0.55)
                            {
                                report.SetSuggestedCluster(bestMatch.Cluster.Id, new ConfidenceScore(Math.Max(0, 1.0 - bestMatch.Distance)));
                            }
                        }

                        if (report.ClusterId == null && report.SuggestedClusterId == null)
                        {
                            // 5. Create new cluster immediately
                            logger.LogInformation("ClusteringBatchConsumer: No cluster match for report {Id}. Creating new cluster.", report.Id);

                            var newCluster = new Cluster(projectId, report.OrganizationId, report.Id);
                            newCluster.UpdateCentroid(report.Embedding!);

                            dbContext.Clusters.Add(newCluster);
                            report.AssignToCluster(newCluster.Id);

                            // Important: Add to projectClusters so other reports in THIS batch can join it!
                            projectClusters.Add(newCluster);
                            modifiedClusterIds.Add(newCluster.Id);
                        }
                    }
                }

                // 6. Save report assignments and new clusters for this project
                await dbContext.SaveChangesAsync(cancellationToken);

                // 7. Recalculate centroids
                foreach (var clusterId in modifiedClusterIds)
                {
                    await clusterService.RecalculateCentroidAsync(clusterId, cancellationToken);
                }

                // 8. Trigger Summary if critical mass reached
                foreach (var clusterId in modifiedClusterIds)
                {
                    var cluster = await dbContext.Clusters.FindAsync([clusterId], cancellationToken);
                    if (cluster != null && cluster.ReportCount >= 5)
                    {
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
            }
        }
    }
}

internal sealed record ClusterCandidate(Cluster Cluster, double Distance);
