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
public class UngroupReportRequest
{
    /// <summary>
    /// ID of the report to ungroup.
    /// </summary>
    public Guid Id { get; set; }
}

public sealed class UngroupReportEndpoint(WinnowDbContext db, IClusterService clusterService) : Endpoint<UngroupReportRequest, ActionResponse>
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
        // Get user ID from JWT
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) ThrowError("Unauthorized", 401);

        // Get project ID from header
        if (!HttpContext.Request.Headers.TryGetValue("X-Project-ID", out var projectIdHeader))
        {
            ThrowError("Project ID is required in X-Project-ID header", 400);
        }

        if (!Guid.TryParse(projectIdHeader, out var projectId))
        {
            ThrowError("Invalid Project ID format", 400);
        }

        // Validate user owns this project
        var userOwnsProject = await db.Projects
            .AsNoTracking()
            .AnyAsync(p => p.Id == projectId && p.OwnerId == userId, ct);

        if (!userOwnsProject)
        {
            ThrowError("Project not found or access denied", 404);
        }

        var report = await db.Reports
            .FirstOrDefaultAsync(r => r.Id == req.Id && r.ProjectId == projectId, ct);

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
        report.ClusterId = null;
        report.Status = "New";

        await db.SaveChangesAsync(ct);

        // If the cluster is still active (has reports), recalculate centroid
        if (oldClusterId != null)
        {
            var remainingCount = await db.Reports
                .CountAsync(r => r.ClusterId == oldClusterId, ct);

            if (remainingCount == 0)
            {
                var cluster = await db.Clusters.FindAsync([oldClusterId], ct);
                if (cluster != null)
                {
                    db.Clusters.Remove(cluster);
                    await db.SaveChangesAsync(ct);
                }
            }
            else
            {
                await clusterService.RecalculateCentroidAsync(oldClusterId.Value, ct);
                await db.SaveChangesAsync(ct);
            }
        }

        await Send.OkAsync(new ActionResponse { Message = "Report ungrouped successfully." }, ct);
    }
}
