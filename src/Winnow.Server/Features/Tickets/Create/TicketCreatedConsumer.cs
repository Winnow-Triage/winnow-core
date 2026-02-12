using System.Linq;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Winnow.Integrations;
using Winnow.Server.Infrastructure.MultiTenancy;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Tickets.Create;

public class TicketCreatedConsumer(
    WinnowDbContext dbContext,
    ITicketExporter exporter,
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
        if (ticket.Status != "Duplicate (StackHash)")
        {
            const double DistanceThreshold = 0.3;

            // Build dynamic filter based on "Critical" Metadata (e.g., Level, BuildVersion)
            // For now, let's filter by Level if it exists.
            string metadataFilter = "";
            var parameters = new List<object> { embeddingBytes };

            if (context.Message.Metadata != null && context.Message.Metadata.TryGetValue("Level", out var levelVal))
            {
                metadataFilter = "AND json_extract(t.MetadataJson, '$.Level') = {1}";
                parameters.Add(levelVal.ToString()!);
            }

            var sql = $@"
                SELECT t.Id, t.ParentTicketId, v.distance
                FROM vec_tickets v
                JOIN Tickets t ON v.rowid = t.rowid
                WHERE v.embedding MATCH {{0}}
                  {metadataFilter}
                  AND k = 5
                ORDER BY distance
            ";

            var searchResults = await dbContext.Database
                .SqlQueryRaw<TicketMatch>(sql, parameters.ToArray())
                .ToListAsync(context.CancellationToken);

            if (searchResults.Any())
            {
                var bestMatch = searchResults[0];
                logger.LogInformation("Best match for ticket {Id}: {MatchId} with distance {Dist}",
                    ticket.Id, bestMatch.Id, bestMatch.Distance);

                if (bestMatch.Distance <= DistanceThreshold)
                {
                    logger.LogInformation("Grouping Ticket {Id} with Parent {ParentId} (Distance: {Dist})",
                        ticket.Id, bestMatch.ParentTicketId ?? bestMatch.Id, bestMatch.Distance);

                    ticket.ParentTicketId = bestMatch.ParentTicketId ?? bestMatch.Id;
                    ticket.Status = "Duplicate";
                    ticket.ConfidenceScore = (float)Math.Max(0, 1.0 - bestMatch.Distance);
                }
            }
        }

        if (ticket.Status != "Duplicate")
        {
            logger.LogInformation("New Unique Ticket {Id}. Exporting to downstream.", ticket.Id);
            await exporter.ExportTicketAsync(ticket.Title, ticket.Description, context.CancellationToken);
            ticket.Status = "Exported";
        }

        // 7. Save the Ticket (and parent assignment if any)
        await dbContext.SaveChangesAsync(context.CancellationToken);

        // 8. Sync to Vector Index
        await dbContext.Database.ExecuteSqlRawAsync(@"
            INSERT INTO vec_tickets(rowid, embedding)
            SELECT rowid, {0}
            FROM Tickets
            WHERE Id = {1}
        ", embeddingBytes, ticket.Id);
    }
}

// Helper Record for the Raw SQL Query
internal record TicketMatch(Guid Id, Guid? ParentTicketId, double Distance);