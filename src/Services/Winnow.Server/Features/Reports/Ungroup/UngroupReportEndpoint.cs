using FastEndpoints;
using Winnow.Server.Features.Shared;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Reports.Ungroup;

public class UngroupReportRequest
{
    public Guid Id { get; set; }
}

public class UngroupReportEndpoint(WinnowDbContext db) : Endpoint<UngroupReportRequest, ActionResponse>
{
    public override void Configure()
    {
        Post("/reports/{id}/ungroup");
        AllowAnonymous();
    }

    public override async Task HandleAsync(UngroupReportRequest req, CancellationToken ct)
    {
        var report = await db.Reports.FindAsync([req.Id], ct);

        if (report == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        if (report.ParentReportId == null)
        {
            ThrowError("Report is not grouped.");
        }

        report.ParentReportId = null;
        report.Status = "New"; 

        await db.SaveChangesAsync(ct);
        await Send.OkAsync(new ActionResponse { Message = "Report ungrouped successfully." }, ct);
    }
}
