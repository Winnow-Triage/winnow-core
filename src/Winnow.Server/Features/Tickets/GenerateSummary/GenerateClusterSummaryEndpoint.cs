using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Entities;
using Winnow.Server.Features.Shared;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Tickets.GenerateSummary;

public class GenerateClusterSummaryRequest
{
    public Guid Id { get; set; }
}

public class GenerateClusterSummaryEndpoint(WinnowDbContext db) : Endpoint<GenerateClusterSummaryRequest, ActionResponse>
{
    private readonly WinnowDbContext _db = db;

    public override void Configure()
    {
        Post("/tickets/{Id}/generate-summary");
        AllowAnonymous();
    }

    public override async Task HandleAsync(GenerateClusterSummaryRequest req, CancellationToken ct)
    {
        var ticket = await _db.Tickets.FindAsync([req.Id], ct);
        if (ticket == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        // Simulating AI delay
        await Task.Delay(1500, ct);

        ticket.Summary = "🤖 (AI Placeholder) This cluster appears to be related to a recurring issue with the payment gateway timeouts observed in the last 24 hours. Recommended action: Check downstream service logs.";

        await _db.SaveChangesAsync(ct);

        await Send.OkAsync(new ActionResponse { Message = "Summary generated successfully." }, ct);
    }
}
