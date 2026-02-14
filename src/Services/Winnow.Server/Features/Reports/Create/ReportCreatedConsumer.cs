using System.Linq;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Winnow.Integrations;
using Winnow.Server.Infrastructure.MultiTenancy;
using Winnow.Server.Infrastructure.Persistence;
using Winnow.Server.Infrastructure.Scheduling;
using Winnow.Server.Features.Reports.Create;

namespace Winnow.Server.Features.Reports.Create;

public class ReportCreatedConsumer(
    WinnowDbContext dbContext,
    ILogger<ReportCreatedConsumer> logger,
    ITenantContext tenantContext,
    Services.Ai.IDuplicateChecker duplicateChecker) : IConsumer<ReportCreatedEvent>
{
    public async Task Consume(ConsumeContext<ReportCreatedEvent> context)
    {
        logger.LogInformation("ReportCreatedConsumer: Consuming message for report {Id} (Tenant: {Tenant})", 
            context.Message.ReportId, context.Message.TenantId);

        if (tenantContext is TenantContext concreteContext)
        {
            concreteContext.TenantId = context.Message.TenantId;
        }

        // 1. Load Report
        var report = await dbContext.Reports.FindAsync([context.Message.ReportId], context.CancellationToken);
        if (report == null) return;

        // 2. Ensure Vector Table
        await dbContext.Database.ExecuteSqlRawAsync(
            "CREATE VIRTUAL TABLE IF NOT EXISTS vec_reports USING vec0(embedding float[384] distance_metric=cosine);",
            context.CancellationToken);

        // 3. Vector Search
        if (report.Embedding != null && report.Status != "Duplicate" && report.Status != "Duplicate (StackHash)")
        {
            var embeddingFloats = new float[report.Embedding.Length / sizeof(float)];
            Buffer.BlockCopy(report.Embedding, 0, embeddingFloats, 0, report.Embedding.Length);
            
            const double DistanceThreshold = 0.35;
            var parameters = new List<object> { report.Embedding };

            var sql = @"
                SELECT t.Id, t.ParentReportId as ParentId, v.distance as Distance
                FROM vec_reports v
                JOIN Reports t ON v.rowid = t.rowid
                WHERE v.embedding MATCH {0}
                  AND k = 50
                ORDER BY v.distance ASC
            ";

            // Using Internal Match helper
            var searchResults = await dbContext.Database
                .SqlQueryRaw<ReportMatch>(sql, parameters.ToArray())
                .ToListAsync(context.CancellationToken);

            logger.LogInformation("ReportMatching: Found {Count} potential matches for report {Id}", searchResults.Count, report.Id);

            if (searchResults.Count > 0)
            {
                var validMatches = searchResults.Where(m => m.Id != report.Id).ToList();

                if (validMatches.Count > 0)
                {
                    var topMatchIds = validMatches.Take(10).Select(m => m.Id).ToList();
                    var clusterMap = await dbContext.Reports
                        .AsNoTracking()
                        .Where(t => topMatchIds.Contains(t.Id))
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
                            .Where(t => t.Id == clusterId || t.ParentReportId == clusterId)
                            .Select(t => t.Embedding)
                            .Where(e => e != null)
                            .ToListAsync(context.CancellationToken);

                        if (members.Count == 0) continue;

                        var centroid = CalculateCentroid(members);
                        var centroidDist = CalculateCosineDistance(embeddingFloats, centroid);

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
                            var parentReport = await dbContext.Reports.FindAsync([bestMatch.Id], context.CancellationToken);
                            if (parentReport != null)
                            {
                                var areDuplicates = await duplicateChecker.AreDuplicatesAsync(
                                    report.Message, report.StackTrace ?? "",
                                    parentReport.Message, parentReport.StackTrace ?? "",
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

        // Sync to Vector Index
        await dbContext.Database.ExecuteSqlRawAsync(@"
            INSERT OR REPLACE INTO vec_reports(rowid, embedding)
            SELECT rowid, {0}
            FROM Reports
            WHERE Id = {1}
        ", report.Embedding, report.Id);
        
        logger.LogDebug("ReportMatching: Synchronized report {Id} to vector index.", report.Id);
    }

    private float[] CalculateCentroid(List<byte[]?> embeddings)
    {
        var validOnes = embeddings.Where(e => e != null).Cast<byte[]>().ToList();
        if (validOnes.Count == 0) return Array.Empty<float>();

        int length = validOnes[0].Length / sizeof(float);
        float[] centroid = new float[length];
        foreach (var bytes in validOnes)
        {
            for (int i = 0; i < length; i++)
            {
                centroid[i] += BitConverter.ToSingle(bytes, i * sizeof(float));
            }
        }
        for (int i = 0; i < length; i++) centroid[i] /= validOnes.Count;
        return centroid;
    }

    private double CalculateCosineDistance(float[] a, float[] b)
    {
        float dot = 0, ma = 0, mb = 0;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            ma += a[i] * a[i];
            mb += b[i] * b[i];
        }
        if (ma == 0 || mb == 0) return 1.0;
        return 1.0 - (dot / (Math.Sqrt(ma) * Math.Sqrt(mb)));
    }
}

internal record ReportMatch(Guid Id, Guid? ParentId, double Distance);
internal record ClusterMatch(Guid Id, double Distance);