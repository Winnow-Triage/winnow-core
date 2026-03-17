using System.Security.Claims;
using FastEndpoints;
using MediatR;
using Winnow.Server.Features.Shared;

namespace Winnow.Server.Features.Clusters.Merge;

public class DismissClusterMergeSuggestionRequest : Winnow.Server.Features.Shared.ProjectScopedRequest
{
    public Guid Id { get; set; }
}

public sealed class DismissClusterMergeSuggestionEndpoint(IMediator mediator)
    : Winnow.Server.Features.Shared.ProjectScopedEndpoint<DismissClusterMergeSuggestionRequest, ActionResponse>
{
    public override void Configure()
    {
        Post("/clusters/{id}/dismiss-merge-suggestion");
        Summary(s =>
        {
            s.Summary = "Dismiss a suggested cluster merge";
            s.Description = "Dismisses the AI-suggested merge for the specified cluster and records a negative match.";
            s.Response<ActionResponse>(200, "Suggestion dismissed");
            s.Response(400, "No pending merge suggestion");
            s.Response(404, "Cluster not found");
        });
        Options(x => x.RequireAuthorization());
    }

    public override async Task HandleAsync(DismissClusterMergeSuggestionRequest req, CancellationToken ct)
    {
        var command = new DismissClusterMergeSuggestionCommand(req.CurrentOrganizationId, req.Id, req.CurrentProjectId);
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

        await Send.OkAsync(new ActionResponse { Message = "Cluster merge suggestion dismissed." }, ct);
    }
}
