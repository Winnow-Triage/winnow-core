using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Entities;
using Winnow.Server.Features.Shared;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Reports.GenerateSummary;

public class GenerateClusterSummaryRequest
{
    public Guid Id { get; set; }
}

public class GenerateClusterSummaryEndpoint(WinnowDbContext db, IClusterSummaryService summaryService) : Endpoint<GenerateClusterSummaryRequest, ActionResponse>
{
    public override void Configure()
    {
        Post("/reports/{Id}/generate-summary");
        AllowAnonymous();
    }

    public override async Task HandleAsync(GenerateClusterSummaryRequest req, CancellationToken ct)
    {
        var report = await db.Reports.FindAsync([req.Id], ct);
        if (report == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        // Fetch related reports (evidence) to provide context for the summary
        var relatedReports = await db.Reports
            .AsNoTracking()
            .Where(t => t.ParentReportId == report.Id)
            .ToListAsync(ct);

        // Convert Report to Ticket (Legacy/Internal) for the service if needed, 
        // but for now, I'll update the service interface if it's strictly using Ticket.
        // Actually, since Report is the new Ticket, I'll update the service to use Report.
        
        var result = await summaryService.GenerateSummaryAsync(relatedReports, ct);

        report.Summary = result.Summary;
        report.CriticalityScore = result.CriticalityScore;
        report.CriticalityReasoning = result.CriticalityReasoning;

        await db.SaveChangesAsync(ct);

        await Send.OkAsync(new ActionResponse { Message = "Summary generated successfully." }, ct);
    }
}
