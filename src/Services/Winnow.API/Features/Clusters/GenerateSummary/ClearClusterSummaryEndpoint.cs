using FastEndpoints;
using MediatR;
using Winnow.API.Features.Shared;

namespace Winnow.API.Features.Clusters.GenerateSummary;

/// <summary>
/// Request to clear an existing summary.
/// </summary>
public class ClearClusterSummaryRequest : ProjectScopedRequest
{
    public Guid Id { get; set; }
}

public sealed class ClearClusterSummaryEndpoint(IMediator mediator) : ProjectScopedEndpoint<ClearClusterSummaryRequest>
{
    public override void Configure()
    {
        Post("/clusters/{Id}/clear-summary");
        Summary(s =>
        {
            s.Summary = "Clear cluster summary";
            s.Description = "Removes the AI-generated summary from a cluster.";
            s.Response(200, "Summary cleared");
            s.Response(404, "Cluster not found");
        });
        Options(x => x.RequireAuthorization());

    }

    public override async Task HandleAsync(ClearClusterSummaryRequest req, CancellationToken ct)
    {
        var command = new ClearClusterSummaryCommand(req.CurrentOrganizationId, req.Id, req.CurrentProjectId);
        var result = await mediator.Send(command, ct);

        if (!result.IsSuccess)
        {
            if (result.StatusCode == 404)
            {
                await Send.NotFoundAsync(ct);
                return;
            }
            ThrowError(result.ErrorMessage ?? "Internal Server Error", result.StatusCode ?? 500);
            return;
        }

        await Send.OkAsync(new { }, ct);
    }
}
