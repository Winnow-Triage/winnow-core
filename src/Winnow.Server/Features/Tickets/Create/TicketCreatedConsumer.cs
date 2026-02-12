using System.Linq;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Winnow.Integrations;
using Winnow.Server.Infrastructure.MultiTenancy;
using Winnow.Server.Infrastructure.Persistence;

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
                    // 2. Find the best candidate
                    // We prefer to link to a PARENT (where ParentTicketId is null) if possible.
                    // But obviously, we can't ignore a 0.0 distance match just because it's a child.

                    // Strategy: Take the top 3 matches. If any of them is a Parent, pick that one. 
                    // Otherwise, pick the closest one.
                    var topCandidates = validMatches.Take(3).ToList();

                    var bestMatch = topCandidates.FirstOrDefault(m => m.ParentTicketId == null)
                                    ?? topCandidates.First();

                    // 3. Resolve the Target ID
                    // If we matched a Child, we want to link to its Parent (Grandparent logic)
                    // If we matched a Parent, we link to it directly.
                    var targetParentId = bestMatch.ParentTicketId ?? bestMatch.Id;

                    logger.LogInformation("Best match is {MatchId} (Dist: {Dist}). Resolved Target Parent: {TargetId}",
                        bestMatch.Id, bestMatch.Distance, targetParentId);

                    if (bestMatch.Distance <= DistanceThreshold)
                    {
                        logger.LogInformation("Grouping {Id} -> {TargetId} (Dist: {Dist})",
                            ticket.Id, targetParentId, bestMatch.Distance);

                        ticket.ParentTicketId = targetParentId;
                        ticket.Status = "Duplicate";
                        ticket.ConfidenceScore = (float)Math.Max(0, 1.0 - bestMatch.Distance);
                    }
                    else if (bestMatch.Distance <= 0.55)
                    {
                        logger.LogInformation("Suggesting {Id} -> {TargetId} (Dist: {Dist})",
                            ticket.Id, targetParentId, bestMatch.Distance);

                        ticket.SuggestedParentId = targetParentId;
                        ticket.SuggestedConfidenceScore = (float)Math.Max(0, 1.0 - bestMatch.Distance);
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
}

// Helper Record for the Raw SQL Query
internal record TicketMatch(Guid Id, Guid? ParentTicketId, double Distance);