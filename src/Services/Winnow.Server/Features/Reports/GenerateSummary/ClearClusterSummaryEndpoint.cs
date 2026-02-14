using FastEndpoints;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Reports.GenerateSummary;

public class ClearClusterSummaryRequest
{
    public Guid Id { get; set; }
}

public class ClearClusterSummaryEndpoint(WinnowDbContext db) : Endpoint<ClearClusterSummaryRequest>
{
    public override void Configure()
    {
        Post("/reports/{Id}/clear-summary");
        AllowAnonymous();
    }

    public override async Task HandleAsync(ClearClusterSummaryRequest req, CancellationToken ct)
    {
        var report = await db.Reports.FindAsync([req.Id], ct);
        if (report == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        report.Summary = null;
        await db.SaveChangesAsync(ct);
        await Send.OkAsync(new { }, ct);
    }
}
