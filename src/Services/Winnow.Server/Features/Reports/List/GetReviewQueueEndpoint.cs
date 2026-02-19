using System.Security.Claims;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Reports.List;

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
