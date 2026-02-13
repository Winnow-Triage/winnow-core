using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Tickets.List;

public record TicketDto(Guid Id, string Title, string Description, string Status, DateTime CreatedAt, Guid? ParentTicketId, float? ConfidenceScore, int? CriticalityScore, string? MetadataJson);

public class ListTicketsEndpoint(WinnowDbContext dbContext) : EndpointWithoutRequest<List<TicketDto>>
{
    public override void Configure()
    {
        Get("/tickets");
        AllowAnonymous(); //TODO: Remove this
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        string sort = HttpContext.Request.Query["sort"].ToString();
        if (string.IsNullOrEmpty(sort)) sort = "newest";

        var query = dbContext.Tickets.AsNoTracking();

        query = sort switch
        {
            "criticality" => query.OrderByDescending(t => t.CriticalityScore).ThenByDescending(t => t.CreatedAt),
            "confidence" => query.OrderByDescending(t => t.ConfidenceScore).ThenByDescending(t => t.CreatedAt),
            _ => query.OrderByDescending(t => t.CreatedAt)
        };

        var tickets = await query
            .Select(t => new TicketDto(t.Id, t.Title, t.Description, t.Status, t.CreatedAt, t.ParentTicketId, t.ConfidenceScore, t.CriticalityScore, t.MetadataJson))
            .ToListAsync(ct);

        await Send.OkAsync(tickets, ct);
    }
}
