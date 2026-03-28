using System.Security.Claims;
using FastEndpoints;
using MediatR;
using Winnow.API.Features.Shared;

namespace Winnow.API.Features.Clusters.Merge;

public class AcceptClusterMergeSuggestionRequest : Winnow.API.Features.Shared.ProjectScopedRequest
{
    public Guid Id { get; set; }
}

public sealed class AcceptClusterMergeSuggestionEndpoint(IMediator mediator)
    : Winnow.API.Features.Shared.ProjectScopedEndpoint<AcceptClusterMergeSuggestionRequest, ActionResponse>
{
    public override void Configure()
    {
        Post("/clusters/{id}/accept-merge-suggestion");
        Summary(s =>
        {
            s.Summary = "Accept a suggested cluster merge";
            s.Description = "Accepts the AI-suggested merge for the specified cluster, moving all reports to the target cluster and marking the source as merged.";
            s.Response<ActionResponse>(200, "Merge accepted");
            s.Response(400, "No pending merge suggestion");
            s.Response(404, "Cluster not found");
        });
        Options(x => x.RequireAuthorization());
    }

    public override async Task HandleAsync(AcceptClusterMergeSuggestionRequest req, CancellationToken ct)
    {
        var command = new AcceptClusterMergeSuggestionCommand(req.CurrentOrganizationId, req.Id, req.CurrentProjectId);
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

        await Send.OkAsync(new ActionResponse { Message = "Cluster merge accepted successfully." }, ct);
    }
}
