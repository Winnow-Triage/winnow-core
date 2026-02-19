using System.Security.Claims;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Reports.List;

/// <summary>
/// Item in the review queue requiring attention.
/// </summary>
/// <param name="ReportId">ID of the report to review.</param>
/// <param name="ReportTitle">Title of the report.</param>
/// <param name="ReportMessage">Message of the report.</param>
/// <param name="ReportStackTrace">Stack trace of the report.</param>
/// <param name="ReportAssignedTo">User assigned to the report.</param>
/// <param name="ReportCreatedAt">Creation timestamp.</param>
/// <param name="SuggestedParentId">ID of the suggested parent report.</param>
/// <param name="SuggestedParentTitle">Title of the suggested parent.</param>
/// <param name="SuggestedParentMessage">Message of the suggested parent.</param>
/// <param name="SuggestedParentStackTrace">Stack trace of the suggested parent.</param>
/// <param name="ConfidenceScore">Confidence in the suggestion.</param>
public record ReviewItemDto(
    Guid ReportId,
    string ReportTitle,
    string ReportMessage,
    string? ReportStackTrace,
    string ReportAssignedTo,
    DateTime ReportCreatedAt,
    Guid SuggestedParentId,
    string SuggestedParentTitle,
    string SuggestedParentMessage,
    string? SuggestedParentStackTrace,
    float? ConfidenceScore
);

public sealed class GetReviewQueueEndpoint(WinnowDbContext db) : EndpointWithoutRequest<List<ReviewItemDto>>
{
    public override void Configure()
    {
        Get("/reports/review-queue");
        Summary(s =>
        {
            s.Summary = "Get backlog review queue";
            s.Description = "Retrieves reports that have suggested parents (clusters) pending review.";
            s.Response<List<ReviewItemDto>>(200, "List of review items");
            s.Response(401, "Unauthorized");
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
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

        var items = await db.Reports.AsNoTracking()
            .Where(t => t.ProjectId == projectId && t.SuggestedParentId != null && t.Status != "Duplicate")
            .Join(db.Reports.Where(p => p.ProjectId == projectId),
                t => t.SuggestedParentId,
                p => p.Id,
                (t, p) => new { Report = t, Parent = p })
            .OrderByDescending(x => x.Report.SuggestedConfidenceScore)
            .Select(x => new ReviewItemDto(
                x.Report.Id,
                x.Report.Title,
                x.Report.Message,
                x.Report.StackTrace,
                x.Report.AssignedTo ?? "Unassigned",
                x.Report.CreatedAt,
                x.Parent.Id,
                x.Parent.Title,
                x.Parent.Message,
                x.Parent.StackTrace,
                x.Report.SuggestedConfidenceScore
            ))
            .ToListAsync(ct);

        await Send.OkAsync(items, ct);
    }
}
