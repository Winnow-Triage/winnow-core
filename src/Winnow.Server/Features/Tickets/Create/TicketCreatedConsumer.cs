using System.Linq;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Winnow.Integrations;
using Winnow.Server.Infrastructure.MultiTenancy;
using Winnow.Server.Infrastructure.Persistence;
using Winnow.Server.Infrastructure.Scheduling;

namespace Winnow.Server.Features.Tickets.Create;

public class TicketCreatedConsumer(
    WinnowDbContext dbContext,
    Winnow.Server.Infrastructure.Integrations.ExporterFactory exporterFactory,
    ILogger<TicketCreatedConsumer> logger,
    ITenantContext tenantContext,
    Services.Ai.IEmbeddingService embeddingService) : IConsumer<TicketCreatedEvent>
{
    public async Task Consume(ConsumeContext<TicketCreatedEvent> context)
    {
        // 1. Hydrate Tenant Context (Crucial for Background Workers!)
        if (tenantContext is TenantContext concreteContext)
        {
            concreteContext.TenantId = context.Message.TenantId;
        }

        // Now that TenantId is set, we can get the correct exporter
        var exporter = await exporterFactory.GetExporterAsync(context.CancellationToken);

        // 2. Load Ticket
        var ticket = await dbContext.Tickets.FindAsync([context.Message.TicketId], context.CancellationToken);
        if (ticket == null) return;

        // 3. Generate Embedding (Simpler content for better matching)
        var contentToEmbed = $"{ticket.Title}\n{ticket.Description}";
        var embeddingFloats = await embeddingService.GetEmbeddingAsync(contentToEmbed);

        // 4. Convert to BLOB for SQLite (Raw Byte Copy)
        var embeddingBytes = new byte[embeddingFloats.Length * sizeof(float)];
        Buffer.BlockCopy(embeddingFloats, 0, embeddingBytes, 0, embeddingBytes.Length);

        ticket.Embedding = embeddingBytes;

        // 5. Ensure Vector Table
        await dbContext.Database.ExecuteSqlRawAsync(
            "CREATE VIRTUAL TABLE IF NOT EXISTS vec_tickets USING vec0(embedding float[384] distance_metric=cosine);",
            context.CancellationToken);

        // 6. Vector Search (Skip if already matched by StackHash)
        if (ticket.Status != "Duplicate" && ticket.Status != "Duplicate (StackHash)")
        {
            const double DistanceThreshold = 0.35;
            var parameters = new List<object> { embeddingBytes };

            // Single-pass raw SQL join is the most reliable for hidden rowids in SQLite + sqlite-vec
            var sql = @"
                SELECT t.Id, t.ParentTicketId, v.distance as Distance
                FROM vec_tickets v
                JOIN Tickets t ON v.rowid = t.rowid
                WHERE v.embedding MATCH {0}
                  AND k = 50
                ORDER BY v.distance ASC
            ";

            var searchResults = await dbContext.Database
                .SqlQueryRaw<TicketMatch>(sql, parameters.ToArray())
                .ToListAsync(context.CancellationToken);

            logger.LogInformation("Vector search for ticket {Id} found {Count} potential matches.", ticket.Id, searchResults.Count);

            if (searchResults.Count > 0)
            {
                // 1. Filter out self
                var validMatches = searchResults.Where(m => m.Id != ticket.Id).ToList();

                if (validMatches.Count > 0)
                {
                    // 2. Identify candidate clusters (based on ultimate parents of top matches)
                    var topMatchIds = validMatches.Take(10).Select(m => m.Id).ToList();
                    var clusterMap = await dbContext.Tickets
                        .AsNoTracking()
                        .Where(t => topMatchIds.Contains(t.Id))
                        .Select(t => new { t.Id, t.ParentTicketId })
                        .ToDictionaryAsync(t => t.Id, t => t.ParentTicketId ?? t.Id, context.CancellationToken);

                    var candidateClusterIds = validMatches.Take(10)
                        .Select(m => clusterMap[m.Id])
                        .Distinct()
                        .ToList();

                    ClusterMatch? bestMatch = null;

                    foreach (var clusterId in candidateClusterIds)
                    {
                        // Fetch all members of this cluster to calculate centroid
                        var members = await dbContext.Tickets
                            .AsNoTracking()
                            .Where(t => t.Id == clusterId || t.ParentTicketId == clusterId)
                            .Select(t => t.Embedding)
                            .Where(e => e != null)
                            .ToListAsync(context.CancellationToken);

                        if (members.Count == 0) continue;

                        // Calculate Centroid (Average Vector)
                        var centroid = CalculateCentroid(members);

                        // Calculate Distance to Centroid
                        var centroidDist = CalculateCosineDistance(embeddingFloats, centroid);

                        logger.LogInformation("Centroid Test: Cluster {ClusterId} has {Count} members. Distance: {Dist:F3}",
                            clusterId, members.Count, centroidDist);

                        if (bestMatch == null || centroidDist < bestMatch.Distance)
                        {
                            bestMatch = new ClusterMatch(clusterId, null, centroidDist);
                        }
                    }

                    if (bestMatch != null && bestMatch.Id != ticket.Id)
                    {
                        // Hierarchy Guard: Ensure we always link to the absolute ROOT
                        var targetParentId = await dbContext.ResolveUltimateMasterAsync(bestMatch.Id, context.CancellationToken);

                        if (bestMatch.Distance <= DistanceThreshold)
                        {
                            logger.LogInformation("Grouping {Id} -> {TargetId} (Centroid Match, Dist: {Dist:F3})",
                                ticket.Id, targetParentId, bestMatch.Distance);

                            ticket.ParentTicketId = targetParentId;
                            ticket.Status = "Duplicate";
                            ticket.ConfidenceScore = (float)Math.Max(0, 1.0 - bestMatch.Distance);
                        }
                        else if (bestMatch.Distance <= 0.55)
                        {
                            logger.LogInformation("Suggesting {Id} -> {TargetId} (Centroid Suggest, Dist: {Dist:F3})",
                                ticket.Id, targetParentId, bestMatch.Distance);

                            ticket.SuggestedParentId = targetParentId;
                            ticket.SuggestedConfidenceScore = (float)Math.Max(0, 1.0 - bestMatch.Distance);
                        }
                    }
                }
            }
        }
        else if (ticket.Status == "Duplicate (StackHash)" && ticket.ConfidenceScore == null)
        {
            ticket.ConfidenceScore = 1.0f;
        }

        if (ticket.Status != "Duplicate" && ticket.Status != "Duplicate (StackHash)" && ticket.Status != "Exported")
        {
            logger.LogInformation("New Unique Ticket {Id}. Exporting to downstream.", ticket.Id);
            try
            {
                await exporter.ExportTicketAsync(ticket.Title, ticket.Description, context.CancellationToken);
                ticket.Status = "Exported";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to export ticket {Id}", ticket.Id);
            }
        }

        // 7. Save the Ticket (and parent assignment if any)
        await dbContext.SaveChangesAsync(context.CancellationToken);

        // 8. Sync to Vector Index
        var affected = await dbContext.Database.ExecuteSqlRawAsync(@"
            INSERT INTO vec_tickets(rowid, embedding)
            SELECT rowid, {0}
            FROM Tickets
            WHERE Id = {1}
        ", embeddingBytes, ticket.Id);

        logger.LogInformation("Ticket {Id} synced to vector index. Content length: {Bytes}. Affected rows: {Aff}.",
            ticket.Id, embeddingBytes.Length, affected);
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

// Helper Record for the Raw SQL Query
internal record TicketMatch(Guid Id, Guid? ParentTicketId, double Distance);
internal record ClusterMatch(Guid Id, Guid? ParentTicketId, double Distance);