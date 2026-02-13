using FastEndpoints;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Tickets.GenerateSummary;

public class ClearClusterSummaryRequest
{
    public Guid Id { get; set; }
}

public class ClearClusterSummaryEndpoint(WinnowDbContext db) : Endpoint<ClearClusterSummaryRequest>
{
    public override void Configure()
    {
        Post("/tickets/{Id}/clear-summary");
        AllowAnonymous();
    }

    public override async Task HandleAsync(ClearClusterSummaryRequest req, CancellationToken ct)
    {
        var ticket = await db.Tickets.FindAsync([req.Id], ct);
        if (ticket == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        ticket.Summary = null;
        await db.SaveChangesAsync(ct);
        await Send.OkAsync(new { }, ct);
    }
}
