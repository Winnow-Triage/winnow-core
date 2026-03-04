using System.Security.Claims;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Reports.List;

/// <summary>
/// Item in the review queue requiring attention.
/// </summary>
public record ReviewItemDto(
    Guid SourceId,
    string SourceTitle,
    string SourceMessage,
    string? SourceStackTrace,
    string SourceAssignedTo,
    DateTime SourceCreatedAt,
    Guid TargetId,
    string? TargetTitle,
    string? TargetSummary,
    float? ConfidenceScore,
    string Type // "Report" or "Cluster"
);

public sealed class GetReviewQueueEndpoint(WinnowDbContext db) : EndpointWithoutRequest<List<ReviewItemDto>>
{
    public override void Configure()
    {
        Get("/reports/review-queue");
        Summary(s =>
        {
            s.Summary = "Get backlog review queue";
            s.Description = "Retrieves reports and clusters that have suggested parents/merges pending review.";
            s.Response<List<ReviewItemDto>>(200, "List of review items");
            s.Response(401, "Unauthorized");
        });
        Options(x => x.RequireAuthorization());
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) ThrowError("Unauthorized", 401);

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

        if (!userOwnsProject) ThrowError("Project not found or access denied", 404);

        // 1. Fetch Report Suggestions
        var reportItems = await db.Reports.AsNoTracking()
            .Where(r => r.ProjectId == projectId && r.SuggestedClusterId != null && r.Status != "Duplicate")
            .Join(db.Clusters.Where(c => c.ProjectId == projectId),
                r => r.SuggestedClusterId,
                c => c.Id,
                (r, c) => new ReviewItemDto(
                    r.Id,
                    r.Title,
                    r.Message,
                    r.StackTrace == r.Message ? null : r.StackTrace,
                    r.AssignedTo ?? "Unassigned",
                    r.CreatedAt,
                    c.Id,
                    c.Title,
                    c.Summary,
                    r.SuggestedConfidenceScore,
                    "Report"
                ))
            .ToListAsync(ct);

        var clusterItems = await db.Clusters.AsNoTracking()
            .Where(c => c.ProjectId == projectId && c.SuggestedMergeClusterId != null && c.Status != "Closed")
            .Join(db.Clusters.Where(c => c.ProjectId == projectId),
                c1 => c1.SuggestedMergeClusterId,
                c2 => c2.Id,
                (c1, c2) => new ReviewItemDto(
                    c1.Id,
                    c1.Title ?? "Untitled Cluster",
                    c1.Summary ?? "No summary available",
                    null,
                    c1.AssignedTo ?? "Unassigned",
                    c1.CreatedAt,
                    c2.Id,
                    c2.Title,
                    c2.Summary,
                    c1.SuggestedMergeConfidenceScore,
                    "Cluster"
                ))
            .ToListAsync(ct);

        // 3. Combine and Sort
        var allItems = reportItems.Concat(clusterItems)
            .OrderByDescending(x => x.ConfidenceScore)
            .ToList();

        await Send.OkAsync(allItems, ct);
    }
}
