using System.Security.Claims;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Reports.GenerateSummary;

/// <summary>
/// Request to clear an existing summary.
/// </summary>
public class ClearClusterSummaryRequest
{
    /// <summary>
    /// ID of the report/cluster.
    /// </summary>
    public Guid Id { get; set; }
}

public sealed class ClearClusterSummaryEndpoint(WinnowDbContext db) : Endpoint<ClearClusterSummaryRequest>
{
    public override void Configure()
    {
        Post("/reports/{Id}/clear-summary");
        Summary(s =>
        {
            s.Summary = "Clear cluster summary";
            s.Description = "Removes the AI-generated summary from a report.";
            s.Response(200, "Summary cleared");
            s.Response(404, "Report not found");
        });
    }

    public override async Task HandleAsync(ClearClusterSummaryRequest req, CancellationToken ct)
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

        report.Summary = null;
        await db.SaveChangesAsync(ct);
        await Send.OkAsync(new { }, ct);
    }
}
