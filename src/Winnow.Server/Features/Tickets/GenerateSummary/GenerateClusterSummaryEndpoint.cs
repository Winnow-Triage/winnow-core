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

public class GenerateClusterSummaryEndpoint(WinnowDbContext db, IClusterSummaryService summaryService) : Endpoint<GenerateClusterSummaryRequest, ActionResponse>
{
    private readonly WinnowDbContext _db = db;
    private readonly IClusterSummaryService _summaryService = summaryService;

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

        // Fetch related tickets (evidence) to provide context for the summary
        var relatedTickets = await _db.Tickets
            .AsNoTracking()
            .Where(t => t.ParentTicketId == ticket.Id)
            .ToListAsync(ct);

        var summary = await _summaryService.GenerateSummaryAsync(relatedTickets, ct);

        ticket.Summary = summary;

        await _db.SaveChangesAsync(ct);

        await Send.OkAsync(new ActionResponse { Message = "Summary generated successfully." }, ct);
    }
}
