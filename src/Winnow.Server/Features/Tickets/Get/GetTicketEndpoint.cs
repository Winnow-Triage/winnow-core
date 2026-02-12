using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Tickets.Get;

public class GetTicketRequest
{
    public Guid Id { get; set; }
}

public class GetTicketResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? ParentTicketId { get; set; }
    public string? AssignedTo { get; set; }
    public string? Summary { get; set; }
    public string? ParentTicketTitle { get; set; }

    // For now, let's include children simple IDs/Titles if it is a parent
    public List<RelatedTicketDto> Evidence { get; set; } = [];
}

public class RelatedTicketDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}

public class GetTicketEndpoint(WinnowDbContext db) : Endpoint<GetTicketRequest, GetTicketResponse>
{
    public override void Configure()
    {
        Get("/tickets/{id}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(GetTicketRequest req, CancellationToken ct)
    {
        var ticket = await db.Tickets
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == req.Id, ct);

        if (ticket == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        // If this ticket is a parent (ParentTicketId is null), find its children (Evidence)
        // If this ticket is a child, maybe we show siblings? 
        // For now, let's assume we are viewing a Cluster (Parent) mostly, but handle both.

        var evidence = new List<RelatedTicketDto>();

        if (ticket.ParentTicketId == null)
        {
            // It might be a cluster parent. Find tickets that point to this one.
            var children = await db.Tickets
                .AsNoTracking()
                .Where(t => t.ParentTicketId == ticket.Id)
                .Select(t => new RelatedTicketDto
                {
                    Id = t.Id,
                    Title = t.Title,
                    Status = t.Status,
                    CreatedAt = t.CreatedAt
                })
                .ToListAsync(ct);

            evidence.AddRange(children);
        }
        else
        {
            // It is a child. Ideally we show the Parent link?
            // The frontend can handle fetching the parent if needed via ParentTicketId.
        }

        string? parentTicketTitle = null;
        if (ticket.ParentTicketId != null)
        {
            var parent = await db.Tickets
                .AsNoTracking()
                .Where(t => t.Id == ticket.ParentTicketId)
                .Select(t => t.Title)
                .FirstOrDefaultAsync(ct);
            parentTicketTitle = parent;
        }

        await Send.OkAsync(new GetTicketResponse
        {
            Id = ticket.Id,
            Title = ticket.Title,
            Description = ticket.Description,
            Status = ticket.Status,
            CreatedAt = ticket.CreatedAt,
            ParentTicketId = ticket.ParentTicketId,
            AssignedTo = ticket.AssignedTo,
            Summary = ticket.Summary,
            ParentTicketTitle = parentTicketTitle,
            Evidence = evidence
        }, ct);
    }
}
