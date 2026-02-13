using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Entities;
using Winnow.Server.Features.Shared;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Tickets.Merge;

public class MergeClustersRequest
{
    public Guid Id { get; set; } // Target Cluster (Parent)
    public List<Guid> SourceIds { get; set; } = new(); // Source Clusters/Tickets to merge INTO target
}

public class MergeClustersEndpoint(WinnowDbContext db) : Endpoint<MergeClustersRequest, ActionResponse>
{
    public override void Configure()
    {
        Post("/tickets/{Id}/merge");
        AllowAnonymous();
    }

    public override async Task HandleAsync(MergeClustersRequest req, CancellationToken ct)
    {
        var targetTicket = await db.Tickets.FindAsync([req.Id], ct);
        if (targetTicket == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        foreach (var sourceId in req.SourceIds)
        {
            if (sourceId == req.Id) continue;

            // 1. Find the source ticket
            var sourceTicket = await db.Tickets.FindAsync([sourceId], ct);
            if (sourceTicket == null) continue;

            // 2. Re-parent the source ticket itself
            sourceTicket.ParentTicketId = targetTicket.Id;
            sourceTicket.Status = "Duplicate";

            // 3. Re-parent all children of the source ticket
            var children = await db.Tickets
                .Where(t => t.ParentTicketId == sourceId)
                .ToListAsync(ct);

            foreach (var child in children)
            {
                child.ParentTicketId = targetTicket.Id;
            }
        }

        await db.SaveChangesAsync(ct);

        await Send.OkAsync(new ActionResponse { Message = "Clusters merged successfully." }, ct);
    }
}
