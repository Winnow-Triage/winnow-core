using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Domain.Reports.ValueObjects;
using Winnow.Server.Features.Shared;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Reports.Assign;

/// <summary>
/// Request to assign a report to a user.
/// </summary>
public class AssignReportRequest : ProjectScopedRequest
{
    /// <summary>
    /// ID of the report to assign.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Username or ID of the assignee. Set to null to unassign.
    /// </summary>
    public string? AssignedTo { get; set; }
}

public sealed class AssignReportEndpoint(WinnowDbContext db) : ProjectScopedEndpoint<AssignReportRequest, ActionResponse>
{
    public override void Configure()
    {
        Post("/reports/{id}/assign");
        Summary(s =>
        {
            s.Summary = "Assign a report";
            s.Description = "Assigns a report to a user. If the report was New, status changes to In Progress.";
            s.Response<ActionResponse>(200, "Report assigned successfully");
            s.Response(404, "Report not found");
        });
        Options(x => x.RequireAuthorization());
    }

    public override async Task HandleAsync(AssignReportRequest req, CancellationToken ct)
    {
        var report = await db.Reports
            .FirstOrDefaultAsync(r => r.Id == req.Id && r.ProjectId == req.CurrentProjectId, ct);

        if (report == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        report.AssignTo(req.AssignedTo);


        await db.SaveChangesAsync(ct);
        await Send.OkAsync(new ActionResponse { Message = $"Report assigned to {req.AssignedTo ?? "Unassigned"}" }, ct);
    }
}
