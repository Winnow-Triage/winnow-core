using System.Security.Claims;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Domain.Clusters.ValueObjects;
using Winnow.Server.Domain.Reports.ValueObjects;
using Winnow.Server.Features.Reports.List;
using Winnow.Server.Features.Shared;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Reports.List;

public class GetReviewQueueRequest : ProjectScopedRequest { }

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

public sealed class GetReviewQueueEndpoint(WinnowDbContext db) : ProjectScopedEndpoint<GetReviewQueueRequest, List<ReviewItemDto>>
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

    public override async Task HandleAsync(GetReviewQueueRequest req, CancellationToken ct)
    {
        // 1. Fetch Report Suggestions
        var reportItems = await db.Reports.AsNoTracking()
            .Where(r => r.ProjectId == req.CurrentProjectId && r.SuggestedClusterId != null && r.Status != ReportStatus.Dismissed)
            .Join(db.Clusters.Where(c => c.ProjectId == req.CurrentProjectId),
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
                    r.SuggestedConfidenceScore != null ? (float?)r.SuggestedConfidenceScore.Value.Score : null,
                    "Report"
                ))
            .ToListAsync(ct);

        var clusterItems = await db.Clusters.AsNoTracking()
            .Where(c => c.ProjectId == req.CurrentProjectId && c.SuggestedMergeClusterId != null && c.Status == ClusterStatus.Open)
            .Join(db.Clusters.Where(c => c.ProjectId == req.CurrentProjectId),
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
                    c1.SuggestedMergeConfidenceScore != null ? (float?)c1.SuggestedMergeConfidenceScore.Value.Score : null,
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
