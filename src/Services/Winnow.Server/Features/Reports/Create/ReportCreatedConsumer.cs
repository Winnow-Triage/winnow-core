using System.Linq;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Winnow.Integrations;
using Winnow.Server.Domain.Services;
using Winnow.Server.Infrastructure.MultiTenancy;
using Winnow.Server.Infrastructure.Persistence;
using Winnow.Server.Infrastructure.Scheduling;
using Winnow.Server.Features.Reports.Create;

namespace Winnow.Server.Features.Reports.Create;

public class ReportCreatedConsumer(
    WinnowDbContext dbContext,
    ILogger<ReportCreatedConsumer> logger,
    ITenantContext tenantContext,
    Services.Ai.IDuplicateChecker duplicateChecker,
    IVectorCalculator vectorCalculator) : IConsumer<ReportCreatedEvent>
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

        // 2. Ensure Vector Table
        await dbContext.Database.ExecuteSqlRawAsync(
            "CREATE VIRTUAL TABLE IF NOT EXISTS vec_reports USING vec0(embedding float[384] distance_metric=cosine);",
            context.CancellationToken);

        // 3. Vector Search - ONLY within the same project
        if (report.Embedding != null && report.Status != "Duplicate" && report.Status != "Duplicate (StackHash)")
        {
            var embeddingFloats = new float[report.Embedding.Length / sizeof(float)];
            Buffer.BlockCopy(report.Embedding, 0, embeddingFloats, 0, report.Embedding.Length);
            
            const double DistanceThreshold = 0.35;
            var parameters = new List<object> { report.Embedding, projectId };

            var sql = @"
                SELECT t.Id, t.ParentReportId as ParentId, v.distance as Distance
                FROM vec_reports v
                JOIN Reports t ON v.rowid = t.rowid
                WHERE v.embedding MATCH {0}
                  AND t.ProjectId = {1}
                  AND k = 50
                ORDER BY v.distance ASC
            ";

            // Using Internal Match helper
            var searchResults = await dbContext.Database
                .SqlQueryRaw<ReportMatch>(sql, [.. parameters])
                .ToListAsync(context.CancellationToken);

            logger.LogInformation("ReportMatching: Found {Count} potential matches for report {Id} in project {ProjectId}", 
                searchResults.Count, report.Id, projectId);

            if (searchResults.Count > 0)
            {
                var validMatches = searchResults.Where(m => m.Id != report.Id).ToList();

                if (validMatches.Count > 0)
                {
                    var topMatchIds = validMatches.Take(10).Select(m => m.Id).ToList();
                    var clusterMap = await dbContext.Reports
                        .AsNoTracking()
                        .Where(t => topMatchIds.Contains(t.Id) && t.ProjectId == projectId)
                        .Select(t => new { t.Id, t.ParentReportId })
                        .ToDictionaryAsync(t => t.Id, t => t.ParentReportId ?? t.Id, context.CancellationToken);

                    var candidateClusterIds = validMatches.Take(10)
                        .Select(m => clusterMap[m.Id])
                        .Distinct()
                        .ToList();

                    ClusterMatch? bestMatch = null;

                    foreach (var clusterId in candidateClusterIds)
                    {
                        var members = await dbContext.Reports
                            .AsNoTracking()
                            .Where(t => t.ProjectId == projectId && (t.Id == clusterId || t.ParentReportId == clusterId))
                            .Select(t => t.Embedding)
                            .Where(e => e != null)
                            .ToListAsync(context.CancellationToken);

                        if (members.Count == 0) continue;

                        var memberFloats = members
                            .Where(e => e != null)
                            .Select(e => VectorCalculator.BytesToFloats(e!))
                            .ToList();
                        var centroid = vectorCalculator.CalculateCentroid(memberFloats);
                        var centroidDist = vectorCalculator.CalculateCosineDistance(embeddingFloats, centroid);

                        if (bestMatch == null || centroidDist < bestMatch.Distance)
                        {
                            bestMatch = new ClusterMatch(clusterId, centroidDist);
                        }
                    }

                    if (bestMatch != null && bestMatch.Id != report.Id)
                    {
                        var targetParentId = await dbContext.ResolveUltimateMasterAsync(bestMatch.Id, context.CancellationToken);

                        if (bestMatch.Distance <= 0.15)
                        {
                            report.ParentReportId = targetParentId;
                            report.Status = "Duplicate";
                            report.ConfidenceScore = (float)Math.Max(0, 1.0 - bestMatch.Distance);
                        }
                        else if (bestMatch.Distance <= DistanceThreshold)
                        {
                            var parentReport = await dbContext.Reports
                                .FirstOrDefaultAsync(t => t.Id == bestMatch.Id && t.ProjectId == projectId, context.CancellationToken);
                            if (parentReport != null)
                            {
                                var areDuplicates = await duplicateChecker.AreDuplicatesAsync(
                                    report.Title, report.Message,
                                    parentReport.Title, parentReport.Message,
                                    context.CancellationToken);

                                if (areDuplicates)
                                {
                                    report.ParentReportId = targetParentId;
                                    report.Status = "Duplicate";
                                    report.ConfidenceScore = (float)Math.Max(0, 1.0 - bestMatch.Distance);
                                }
                                else
                                {
                                    report.SuggestedParentId = targetParentId;
                                    report.SuggestedConfidenceScore = (float)Math.Max(0, 1.0 - bestMatch.Distance);
                                }
                            }
                        }
                    }
                }
            }
        }

        await dbContext.SaveChangesAsync(context.CancellationToken);

        // Sync to Vector Index - only if embedding exists
        if (report.Embedding != null)
        {
            await dbContext.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT OR REPLACE INTO vec_reports(rowid, embedding)
                SELECT rowid, {report.Embedding}
                FROM Reports
                WHERE Id = {report.Id}
            ");
            
            logger.LogDebug("ReportMatching: Synchronized report {Id} to vector index.", report.Id);
        }
        else
        {
            logger.LogDebug("ReportMatching: Skipping vector index sync for report {Id} - no embedding.", report.Id);
        }
    }
}

internal record ReportMatch(Guid Id, Guid? ParentId, double Distance);
internal record ClusterMatch(Guid Id, double Distance);