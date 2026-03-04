using System.Linq;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Domain.Services;
using Winnow.Server.Entities;
using Winnow.Server.Features.Reports.Create;
using Winnow.Server.Infrastructure.MultiTenancy;
using Winnow.Server.Infrastructure.Persistence;
using Winnow.Server.Services.Ai;

namespace Winnow.Server.Features.Reports.Create;

internal class ReportCreatedConsumer(
    WinnowDbContext dbContext,
    ILogger<ReportCreatedConsumer> logger,
    ITenantContext tenantContext,
    Services.Ai.IDuplicateChecker duplicateChecker,
    IVectorCalculator vectorCalculator,
    IClusterService clusterService) : IConsumer<ReportCreatedEvent>
{
    public async Task Consume(ConsumeContext<ReportCreatedEvent> context)
    {
        logger.LogInformation("ReportCreatedConsumer: Consuming message for report {Id} (Tenant: {Tenant}, Project: {Project})",
            context.Message.ReportId, context.Message.TenantId, context.Message.ProjectId);

        if (tenantContext is TenantContext concreteContext)
        {
            concreteContext.TenantId = context.Message.TenantId;
        }

        // 1. Load Report
        var report = await dbContext.Reports.FindAsync([context.Message.ReportId], context.CancellationToken);
        if (report == null) return;

        // Ensure report has the correct ProjectId from the event
        if (report.ProjectId == Guid.Empty && context.Message.ProjectId != Guid.Empty)
        {
            report.ProjectId = context.Message.ProjectId;
            await dbContext.SaveChangesAsync(context.CancellationToken);
        }

        var projectId = report.ProjectId;

        // 3. Cluster Matching — find best cluster by centroid similarity
        if (report.Embedding != null && report.Status != "Duplicate" && report.Status != "Duplicate (StackHash)")
        {
            var embeddingFloats = report.Embedding;

            // Search existing clusters in this project
            var clusters = await dbContext.Clusters
                .AsNoTracking()
                .Where(c => c.ProjectId == projectId && c.Status != "Closed" && c.Centroid != null)
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
                    report.ClusterId = bestMatch.Id;
                    report.Status = "Duplicate";
                    report.ConfidenceScore = (float)Math.Max(0, 1.0 - bestMatch.Distance);
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
                            report.ClusterId = bestMatch.Id;
                            report.Status = "Duplicate";
                            report.ConfidenceScore = (float)Math.Max(0, 1.0 - bestMatch.Distance);
                        }
                        else
                        {
                            report.SuggestedClusterId = bestMatch.Id;
                            report.SuggestedConfidenceScore = (float)Math.Max(0, 1.0 - bestMatch.Distance);
                        }
                    }
                }
                else if (bestMatch.Distance <= 0.55)
                {
                    // Low similarity: suggest only
                    report.SuggestedClusterId = bestMatch.Id;
                    report.SuggestedConfidenceScore = (float)Math.Max(0, 1.0 - bestMatch.Distance);
                }
            }

            // 4. If no match found, create a new cluster for this report
            if (report.ClusterId == null && report.SuggestedClusterId == null)
            {
                var newCluster = new Cluster
                {
                    ProjectId = projectId,
                    OrganizationId = report.OrganizationId,
                    Centroid = embeddingFloats,
                    Title = report.Title,
                    Status = "Open",
                };
                dbContext.Clusters.Add(newCluster);
                report.ClusterId = newCluster.Id;
            }

        }
        else if (report.Embedding != null && report.ClusterId == null)
        {
            // Report has embedding but is already duplicate/special status — create solo cluster
            var newCluster = new Cluster
            {
                ProjectId = projectId,
                OrganizationId = report.OrganizationId,
                Centroid = report.Embedding,
                Title = report.Title,
                Status = "Open",
            };
            dbContext.Clusters.Add(newCluster);
            report.ClusterId = newCluster.Id;
        }

        await dbContext.SaveChangesAsync(context.CancellationToken);

        // 5. Recalculate cluster centroid AFTER SaveChanges so the new report is included in the query
        if (report.ClusterId != null)
        {
            await clusterService.RecalculateCentroidAsync(report.ClusterId.Value, context.CancellationToken);
            await dbContext.SaveChangesAsync(context.CancellationToken); // Save the updated centroid
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