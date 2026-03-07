using System.Security.Claims;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Features.Shared;
using Winnow.Server.Infrastructure.Persistence;
using Winnow.Server.Services.Ai;

namespace Winnow.Server.Features.Reports.Ungroup;

/// <summary>
/// Request to remove a report from its cluster.
/// </summary>
public class UngroupReportRequest : ProjectScopedRequest
{
    /// <summary>
    /// ID of the report to ungroup.
    /// </summary>
    public Guid Id { get; set; }
}

public sealed class UngroupReportEndpoint(WinnowDbContext db, IClusterService clusterService) : ProjectScopedEndpoint<UngroupReportRequest, ActionResponse>
{
    public override void Configure()
    {
        Post("/reports/{id}/ungroup");
        Summary(s =>
        {
            s.Summary = "Ungroup a report";
            s.Description = "Removes a report from its current cluster and sets its status to New.";
            s.Response<ActionResponse>(200, "Report ungrouped successfully");
            s.Response(400, "Report is not grouped");
            s.Response(404, "Report not found");
        });
        Options(x => x.RequireAuthorization());
    }

    public override async Task HandleAsync(UngroupReportRequest req, CancellationToken ct)
    {
        var report = await db.Reports
            .FirstOrDefaultAsync(r => r.Id == req.Id && r.ProjectId == req.CurrentProjectId, ct);

        if (report == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        if (report.ClusterId == null)
        {
            ThrowError("Report is not grouped.");
        }

        var oldClusterId = report.ClusterId;
        report.RemoveFromCluster();

        await db.SaveChangesAsync(ct);

        if (oldClusterId != null)
        {
            await clusterService.RecalculateCentroidAsync(oldClusterId.Value, ct);
        }

        await Send.OkAsync(new ActionResponse { Message = "Report ungrouped successfully." }, ct);
    }
}
