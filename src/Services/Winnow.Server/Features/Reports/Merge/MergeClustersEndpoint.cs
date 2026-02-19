using System.Security.Claims;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Entities;
using Winnow.Server.Features.Shared;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Reports.Merge;

/// <summary>
/// Request to merge multiple clusters/reports into a target cluster.
/// </summary>
public class MergeClustersRequest
{
    /// <summary>
    /// The target report/cluster ID that others will be merged INTO.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// List of source report IDs to merge into the target.
    /// </summary>
    public List<Guid> SourceIds { get; set; } = new();
}

public sealed class MergeClustersEndpoint(WinnowDbContext db) : Endpoint<MergeClustersRequest, ActionResponse>
{
    public override void Configure()
    {
        Post("/reports/{Id}/merge");
        Summary(s =>
        {
            s.Summary = "Merge clusters";
            s.Description = "Merges multiple source reports/clusters into a single target cluster. Source reports are marked as Duplicate.";
            s.Response<ActionResponse>(200, "Clusters merged successfully");
            s.Response(404, "Target report not found");
        });
    }

    public override async Task HandleAsync(MergeClustersRequest req, CancellationToken ct)
    {
        // Get user ID from JWT
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            ThrowError("Unauthorized", 401);
        }

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

        var targetReport = await db.Reports
            .FirstOrDefaultAsync(r => r.Id == req.Id && r.ProjectId == projectId, ct);
        if (targetReport == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        foreach (var sourceId in req.SourceIds)
        {
            if (sourceId == req.Id) continue;

            var sourceReport = await db.Reports
                .FirstOrDefaultAsync(r => r.Id == sourceId && r.ProjectId == projectId, ct);
            if (sourceReport == null) continue;

            sourceReport.ParentReportId = targetReport.Id;
            sourceReport.Status = "Duplicate";

            var children = await db.Reports
                .Where(t => t.ProjectId == projectId && t.ParentReportId == sourceId)
                .ToListAsync(ct);

            foreach (var child in children)
            {
                child.ParentReportId = targetReport.Id;
            }
        }

        await db.SaveChangesAsync(ct);

        await Send.OkAsync(new ActionResponse { Message = "Clusters merged successfully." }, ct);
    }
}
