using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Tickets.List;

public record TicketDto(Guid Id, string Title, string Description, string Status, DateTime CreatedAt, Guid? ParentTicketId, float? ConfidenceScore);

public class ListTicketsEndpoint(WinnowDbContext dbContext) : EndpointWithoutRequest<List<TicketDto>>
{
    public override void Configure()
    {
        Get("/tickets");
        AllowAnonymous(); //TODO: Remove this
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var tickets = await dbContext.Tickets
            .AsNoTracking()
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new TicketDto(t.Id, t.Title, t.Description, t.Status, t.CreatedAt, t.ParentTicketId, t.ConfidenceScore))
            .ToListAsync(ct);

        await Send.OkAsync(tickets, ct);
    }
}
