using System.Security.Claims;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Features.Shared;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Reports.Assign;

/// <summary>
/// Request to assign a report to a user.
/// </summary>
public class AssignReportRequest
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

public sealed class AssignReportEndpoint(WinnowDbContext db) : Endpoint<AssignReportRequest, ActionResponse>
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
    }

    public override async Task HandleAsync(AssignReportRequest req, CancellationToken ct)
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

        var report = await db.Reports
            .FirstOrDefaultAsync(r => r.Id == req.Id && r.ProjectId == projectId, ct);

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
