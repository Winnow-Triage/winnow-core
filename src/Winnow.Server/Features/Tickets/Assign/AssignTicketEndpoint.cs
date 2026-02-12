using FastEndpoints;
using Winnow.Server.Features.Shared;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Tickets.Assign;

public class AssignTicketRequest
{
    public Guid Id { get; set; }
    public string? AssignedTo { get; set; }
}

public class AssignTicketEndpoint(WinnowDbContext db) : Endpoint<AssignTicketRequest, ActionResponse>
{
    public override void Configure()
    {
        Post("/tickets/{id}/assign");
        AllowAnonymous();
    }

    public override async Task HandleAsync(AssignTicketRequest req, CancellationToken ct)
    {
        var ticket = await db.Tickets.FindAsync([req.Id], ct);

        if (ticket == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        ticket.AssignedTo = req.AssignedTo;
        // Optionally set status to "Assigned" or "In Progress" if needed.
        if (ticket.Status == "New" && !string.IsNullOrEmpty(req.AssignedTo))
        {
            ticket.Status = "In Progress";
        }

        await db.SaveChangesAsync(ct);
        await Send.OkAsync(new ActionResponse { Message = $"Ticket assigned to {req.AssignedTo ?? "Unassigned"}" }, ct);
    }
}
