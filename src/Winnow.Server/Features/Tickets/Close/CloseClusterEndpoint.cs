using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Features.Shared;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Tickets.Close;

public class CloseClusterRequest
{
    public Guid Id { get; set; }
}

public class CloseClusterEndpoint(WinnowDbContext db) : Endpoint<CloseClusterRequest, ActionResponse>
{
    public override void Configure()
    {
        Post("/tickets/{id}/close-cluster");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CloseClusterRequest req, CancellationToken ct)
    {
        var ticket = await db.Tickets.FindAsync([req.Id], ct);

        if (ticket == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        // Determine the Cluster ID (Parent ID)
        var clusterId = ticket.ParentTicketId ?? ticket.Id;

        // Find all tickets in this cluster (Parent + Children)
        var clusterTickets = await db.Tickets
            .Where(t => t.Id == clusterId || t.ParentTicketId == clusterId)
            .ToListAsync(ct);

        foreach (var t in clusterTickets)
        {
            t.Status = "Closed";
        }

        await db.SaveChangesAsync(ct);
        await Send.OkAsync(new ActionResponse { Message = $"Closed {clusterTickets.Count} tickets in cluster." }, ct);
    }
}
