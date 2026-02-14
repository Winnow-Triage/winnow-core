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

public class GetReviewQueueEndpoint(WinnowDbContext db) : EndpointWithoutRequest<List<ReviewItemDto>>
{
    public override void Configure()
    {
        Get("/reports/review-queue");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var items = await db.Reports.AsNoTracking()
            .Where(t => t.SuggestedParentId != null && t.Status != "Duplicate")
            .Join(db.Reports,
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
