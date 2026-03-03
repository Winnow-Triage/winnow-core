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
/// <param name="SuggestedClusterId">ID of the suggested cluster.</param>
/// <param name="SuggestedClusterTitle">Title of the suggested cluster.</param>
/// <param name="SuggestedClusterSummary">Summary of the suggested cluster.</param>
/// <param name="ConfidenceScore">Confidence in the suggestion.</param>
/// <param name="IsOverage">Whether this report exceeded the free limits.</param>
/// <param name="IsLocked">Whether this report was held for ransom due to grace period breach.</param>
public record ReviewItemDto(
    Guid ReportId,
    string ReportTitle,
    string ReportMessage,
    string? ReportStackTrace,
    string ReportAssignedTo,
    DateTime ReportCreatedAt,
    Guid SuggestedClusterId,
    string? SuggestedClusterTitle,
    string? SuggestedClusterSummary,
    float? ConfidenceScore,
    bool IsOverage,
    bool IsLocked
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
            .Where(t => t.ProjectId == projectId && t.SuggestedClusterId != null && t.Status != "Duplicate")
            .Join(db.Clusters.Where(c => c.ProjectId == projectId),
                t => t.SuggestedClusterId,
                c => c.Id,
                (t, c) => new { Report = t, Cluster = c })
            .OrderByDescending(x => x.Report.SuggestedConfidenceScore)
            .Select(x => new ReviewItemDto(
                x.Report.Id,
                x.Report.Title,
                x.Report.Message,
                x.Report.StackTrace,
                x.Report.AssignedTo ?? "Unassigned",
                x.Report.CreatedAt,
                x.Cluster.Id,
                x.Cluster.Title,
                x.Cluster.Summary,
                x.Report.SuggestedConfidenceScore,
                x.Report.IsOverage,
                x.Report.IsLocked
            ))
            .ToListAsync(ct);

        await Send.OkAsync(items, ct);
    }
}
