using FastEndpoints;
using Winnow.Server.Features.Shared;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Reports.Assign;

public class AssignReportRequest
{
    public Guid Id { get; set; }
    public string? AssignedTo { get; set; }
}

public class AssignReportEndpoint(WinnowDbContext db) : Endpoint<AssignReportRequest, ActionResponse>
{
    public override void Configure()
    {
        Post("/reports/{id}/assign");
        AllowAnonymous();
    }

    public override async Task HandleAsync(AssignReportRequest req, CancellationToken ct)
    {
        var report = await db.Reports.FindAsync([req.Id], ct);

        if (report == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        report.AssignedTo = req.AssignedTo;
        if (report.Status == "New" && !string.IsNullOrEmpty(req.AssignedTo))
        {
            report.Status = "In Progress";
        }

        await db.SaveChangesAsync(ct);
        await Send.OkAsync(new ActionResponse { Message = $"Report assigned to {req.AssignedTo ?? "Unassigned"}" }, ct);
    }
}
