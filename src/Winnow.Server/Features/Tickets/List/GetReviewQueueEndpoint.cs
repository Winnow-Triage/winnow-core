using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Tickets.List;

public record ReviewItemDto(
    Guid TicketId,
    string TicketTitle,
    string TicketDescription,
    string TicketAuthor,
    DateTime TicketCreatedAt,
    Guid SuggestedParentId,
    string SuggestedParentTitle,
    string SuggestedParentDescription,
    float? ConfidenceScore
);

public class GetReviewQueueEndpoint(WinnowDbContext db) : EndpointWithoutRequest<List<ReviewItemDto>>
{
    public override void Configure()
    {
        Get("/tickets/review-queue");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var items = await db.Tickets.AsNoTracking()
            .Where(t => t.SuggestedParentId != null && t.Status != "Duplicate")
            .Join(db.Tickets,
                t => t.SuggestedParentId,
                p => p.Id,
                (t, p) => new { Ticket = t, Parent = p })
            .OrderByDescending(x => x.Ticket.SuggestedConfidenceScore)
            .Select(x => new ReviewItemDto(
                x.Ticket.Id,
                x.Ticket.Title,
                x.Ticket.Description,
                x.Ticket.AssignedTo ?? "Unassigned",
                x.Ticket.CreatedAt,
                x.Parent.Id,
                x.Parent.Title,
                x.Parent.Description,
                x.Ticket.SuggestedConfidenceScore
            ))
            .ToListAsync(ct);

        await Send.OkAsync(items, ct);
    }
}
