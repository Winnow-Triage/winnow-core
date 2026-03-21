using MediatR;
using Microsoft.EntityFrameworkCore;
using Winnow.API.Domain.Clusters.ValueObjects;
using Winnow.API.Domain.Reports.ValueObjects;
using Winnow.API.Features.Reports.List;
using Winnow.API.Infrastructure.Persistence;

using Winnow.API.Infrastructure.Security.Authorization;
using Winnow.API.Features.Shared;

namespace Winnow.API.Features.Reports.List;

[RequirePermission("reports:read")]
public record GetReviewQueueQuery(Guid CurrentOrganizationId, Guid ProjectId) : IRequest<List<ReviewItemDto>>, IOrgScopedRequest;

public class GetReviewQueueHandler(WinnowDbContext db) : IRequestHandler<GetReviewQueueQuery, List<ReviewItemDto>>
{
    public async Task<List<ReviewItemDto>> Handle(GetReviewQueueQuery request, CancellationToken cancellationToken)
    {
        // 1. Fetch Report Suggestions
        var reportItems = await db.Reports.AsNoTracking()
            .Where(r => r.ProjectId == request.ProjectId && r.SuggestedClusterId != null && r.Status != ReportStatus.Dismissed && r.IsSanitized)
            .Join(db.Clusters.Where(c => c.ProjectId == request.ProjectId),
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
            .ToListAsync(cancellationToken);

        // 2. Fetch Cluster Merge Suggestions
        var clusterItems = await db.Clusters.AsNoTracking()
            .Where(c => c.ProjectId == request.ProjectId && c.SuggestedMergeClusterId != null && c.Status == ClusterStatus.Open)
            .Join(db.Clusters.Where(c => c.ProjectId == request.ProjectId),
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
            .ToListAsync(cancellationToken);

        // 3. Combine and Sort
        var allItems = reportItems.Concat(clusterItems)
            .OrderByDescending(x => x.ConfidenceScore)
            .ToList();

        return allItems;
    }
}
