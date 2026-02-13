using FastEndpoints;
using Winnow.Server.Features.Shared;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Tickets.Ungroup;

public class UngroupTicketRequest
{
    public Guid Id { get; set; }
}

public class UngroupTicketEndpoint(WinnowDbContext db) : Endpoint<UngroupTicketRequest, ActionResponse>
{
    public override void Configure()
    {
        Post("/tickets/{id}/ungroup");
        AllowAnonymous();
    }

    public override async Task HandleAsync(UngroupTicketRequest req, CancellationToken ct)
    {
        var ticket = await db.Tickets.FindAsync([req.Id], ct);

        if (ticket == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        if (ticket.ParentTicketId == null)
        {
            ThrowError("Ticket is not grouped.");
        }

        ticket.ParentTicketId = null;
        ticket.Status = "New"; // Reset status or keep as is? "New" implies it needs triage again.

        await db.SaveChangesAsync(ct);
        await Send.OkAsync(new ActionResponse { Message = "Ticket ungrouped successfully." }, ct);
    }
}
