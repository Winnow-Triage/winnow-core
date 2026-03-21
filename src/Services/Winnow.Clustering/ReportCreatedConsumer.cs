using MassTransit;
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

namespace Winnow.Clustering;

public sealed class ReportCreatedConsumer(
    WinnowDbContext dbContext,
    ILogger<ReportCreatedConsumer> logger,
    ITenantContext tenantContext,
    IDuplicateChecker duplicateChecker,
    IVectorCalculator vectorCalculator,
    IClusterService clusterService,
    IEmbeddingService embeddingService) : IConsumer<ReportSanitizedEvent>
{
    public async Task Consume(ConsumeContext<ReportSanitizedEvent> context)
    {
        logger.LogInformation("ReportCreatedConsumer: Consuming sanitized report {Id} (Organization: {Organization}, Project: {Project})",
            context.Message.ReportId, context.Message.CurrentOrganizationId, context.Message.ProjectId);

        if (tenantContext is TenantContext concreteContext)
        {
            concreteContext.TenantId = context.Message.CurrentOrganizationId.ToString();
        }

        // 1. Load Report
        var report = await dbContext.Reports.FindAsync([context.Message.ReportId], context.CancellationToken);
        if (report == null) return;

        // 2. Generate embedding if missing (e.g. from legacy reports or sanitize sync lag)
        if (report.Embedding == null)
        {
            logger.LogInformation("ReportCreatedConsumer: Generating missing embedding for report {Id}", report.Id);
            var text = $"{report.Title}\n{report.Message}\n{report.StackTrace}";
            var embedding = await embeddingService.GetEmbeddingAsync(text);
            report.SetEmbedding(embedding);
            // We'll save with the next SaveChanges
        }

        var projectId = report.ProjectId;

        // 3. Cluster Matching — find best cluster by centroid similarity
        if (report.Embedding != null && report.Status != ReportStatus.Dismissed)
        {
            var embeddingFloats = report.Embedding;

            // Search existing clusters in this project
            var clusters = await dbContext.Clusters
                .AsNoTracking()
                .Where(c => c.ProjectId == projectId && c.Status != ClusterStatus.Dismissed && c.Centroid != null)
                .ToListAsync(context.CancellationToken);

            ClusterMatch? bestMatch = null;

            foreach (var cluster in clusters)
            {
                if (cluster.Centroid == null) continue;
                var centroidDist = vectorCalculator.CalculateCosineDistance(embeddingFloats, cluster.Centroid);

                if (bestMatch == null || centroidDist < bestMatch.Distance)
                {
                    bestMatch = new ClusterMatch(cluster.Id, centroidDist);
                }
            }

            if (bestMatch != null)
            {
                if (bestMatch.Distance <= 0.15)
                {
                    // Auto-merge: very high similarity
                    report.AssignToCluster(bestMatch.Id);
                    report.ChangeStatus(ReportStatus.Duplicate); // Duplicate, not Dismissed
                    report.SetConfidenceScore(new ConfidenceScore(Math.Max(0, 1.0 - bestMatch.Distance)));
                }
                else if (bestMatch.Distance <= 0.35)
                {
                    // Medium similarity: AI-confirm before merge
                    var clusterReports = await dbContext.Reports
                        .AsNoTracking()
                        .Where(r => r.ClusterId == bestMatch.Id && r.ProjectId == projectId)
                        .OrderBy(r => r.CreatedAt)
                        .Take(1)
                        .ToListAsync(context.CancellationToken);

                    if (clusterReports.Count > 0)
                    {
                        var representative = clusterReports[0];
                        var areDuplicates = await duplicateChecker.AreDuplicatesAsync(
                            report.Title, report.Message,
                            representative.Title, representative.Message,
                            context.CancellationToken);

                        if (areDuplicates)
                        {
                            report.AssignToCluster(bestMatch.Id);
                            report.ChangeStatus(ReportStatus.Duplicate); // Duplicate, not Dismissed
                            report.SetConfidenceScore(new ConfidenceScore(Math.Max(0, 1.0 - bestMatch.Distance)));
                        }
                        else
                        {
                            report.SetSuggestedCluster(bestMatch.Id, new ConfidenceScore(Math.Max(0, 1.0 - bestMatch.Distance)));
                        }
                    }
                    else
                    {
                        // No representative in the cluster yet, allow merge directly
                        report.AssignToCluster(bestMatch.Id);
                        report.ChangeStatus(ReportStatus.Duplicate);
                        report.SetConfidenceScore(new ConfidenceScore(Math.Max(0, 1.0 - bestMatch.Distance)));
                    }
                }
                else if (bestMatch.Distance <= 0.55)
                {
                    // Low similarity: suggest only
                    report.SetSuggestedCluster(bestMatch.Id, new ConfidenceScore(Math.Max(0, 1.0 - bestMatch.Distance)));
                }
            }

            // 4. If no match found, leave as orphan — the ClusterRefinementJob (Janitor)
            //    will batch-evaluate it in the next cycle and match it against the full
            //    project cluster set, or create a new cluster for it then.
            if (report.ClusterId == null && report.SuggestedClusterId == null)
            {
                logger.LogInformation("ReportCreatedConsumer: No cluster match for report {Id} (best distance: {Distance:F3}). Leaving as orphan for janitor.",
                    report.Id, bestMatch?.Distance ?? 1.0);
            }

        }

        await dbContext.SaveChangesAsync(context.CancellationToken);

        // 5. Recalculate cluster centroid AFTER SaveChanges so the new report is included in the query
        if (report.ClusterId != null)
        {
            await clusterService.RecalculateCentroidAsync(report.ClusterId.Value, context.CancellationToken);
            await dbContext.SaveChangesAsync(context.CancellationToken); // Save the updated centroid

            // 6. Trigger Summarization if we've reached critical mass (e.g. 5 reports)
            // Load the cluster to check its current count and last summary time
            var cluster = await dbContext.Clusters.FindAsync([report.ClusterId.Value], context.CancellationToken);
            if (cluster != null && cluster.ReportCount >= 5)
            {
                var tenMinutesAgo = DateTime.UtcNow.AddMinutes(-10);
                if (cluster.LastSummarizedAt == null || cluster.LastSummarizedAt <= tenMinutesAgo)
                {
                    logger.LogInformation("ReportCreatedConsumer: Cluster {ClusterId} reached critical mass ({Count} reports). Requesting summary.",
                        cluster.Id, cluster.ReportCount);

                    await context.Publish(new GenerateClusterSummaryEvent(
                        cluster.Id,
                        cluster.OrganizationId,
                        cluster.ProjectId), context.CancellationToken);
                }
            }
        }

        if (report.Embedding != null)
        {
            // Postgres handles this natively during SaveChangesAsync! No shadow tables needed.
            logger.LogDebug("ReportMatching: Vector saved natively to Postgres for report {Id}.", report.Id);
        }
        else
        {
            logger.LogDebug("ReportMatching: Skipping vector index sync for report {Id} - no embedding.", report.Id);
        }
    }
}

internal sealed record ClusterMatch(Guid Id, double Distance);