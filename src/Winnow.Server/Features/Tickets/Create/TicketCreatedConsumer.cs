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

        // 3. Generate Embedding (Float array)
        var embeddingFloats = await embeddingService.GetEmbeddingAsync(ticket.Description);

        // 4. Convert to BLOB for SQLite (Raw Byte Copy)
        var embeddingBytes = new byte[embeddingFloats.Length * sizeof(float)];
        Buffer.BlockCopy(embeddingFloats, 0, embeddingBytes, 0, embeddingBytes.Length);

        ticket.Embedding = embeddingBytes;

        // 5. Ensure Vector Table (Idempotent, but ideally move to Startup)
        // We do this raw SQL because EF Core doesn't know about "VIRTUAL TABLE USING vec0"
        await dbContext.Database.ExecuteSqlRawAsync(
            "CREATE VIRTUAL TABLE IF NOT EXISTS vec_tickets USING vec0(embedding float[384]);",
            context.CancellationToken);

        // 6. Save the Ticket FIRST (to get a valid Row ID)
        await dbContext.SaveChangesAsync(context.CancellationToken);

        // 7. Sync to Vector Index
        // Note: We cast the parameter to BLOB to ensure SQLite treats it as raw bytes
        // We MUST use the internal rowid from the Tickets table, NOT the Guid Id
        await dbContext.Database.ExecuteSqlRawAsync(@"
            INSERT INTO vec_tickets(rowid, embedding)
            SELECT rowid, {0}
            FROM Tickets
            WHERE Id = {1}
        ", embeddingBytes, ticket.Id);

        // 8. Vector Search (The "Magic" Step)
        // We use a projection to get the 'distance' back from the virtual table
        // sqlite-vec distance is 'Cosine Distance' (lower is better).
        // 0.3 distance ~= 0.7 similarity.
        const double DistanceThreshold = 0.3;

        var match = await dbContext.Database
            .SqlQueryRaw<TicketMatch>(@"
                SELECT t.Id, t.ParentTicketId, v.distance
                FROM vec_tickets v
                JOIN Tickets t ON v.rowid = t.rowid
                WHERE v.embedding MATCH {0}
                  AND k = 2  -- Get top 2 (The ticket itself is #1, we want #2)
                ORDER BY distance
            ", embeddingBytes)
            .Where(m => m.Id != ticket.Id) // Filter out self-match
            .FirstOrDefaultAsync(context.CancellationToken);

        if (match != null && match.Distance <= DistanceThreshold)
        {
            logger.LogInformation("Grouping Ticket {Id} with Parent {ParentId} (Distance: {Dist})",
                ticket.Id, match.ParentTicketId ?? match.Id, match.Distance);

            ticket.ParentTicketId = match.ParentTicketId ?? match.Id;
            ticket.Status = "Duplicate";

            // Optimization: We don't export duplicates
            await dbContext.SaveChangesAsync(context.CancellationToken);
        }
        else
        {
            logger.LogInformation("New Unique Ticket {Id}. Exporting to downstream.", ticket.Id);

            // Export logic
            await exporter.ExportTicketAsync(ticket.Title, ticket.Description, context.CancellationToken);
            // ticket.ExternalId = exportId;
            ticket.Status = "Exported";

            await dbContext.SaveChangesAsync(context.CancellationToken);
        }
    }
}

// Helper Record for the Raw SQL Query
internal record TicketMatch(Guid Id, Guid? ParentTicketId, double Distance);