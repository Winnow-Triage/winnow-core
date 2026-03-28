using FastEndpoints;
using MediatR;
using Winnow.API.Features.Shared;

namespace Winnow.API.Features.Reports.List;

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

public sealed class GetReviewQueueEndpoint(IMediator mediator) : ProjectScopedEndpoint<GetReviewQueueRequest, List<ReviewItemDto>>
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
        var query = new GetReviewQueueQuery(req.CurrentOrganizationId, req.CurrentProjectId);
        var result = await mediator.Send(query, ct);

        await Send.OkAsync(result, ct);
    }
}
