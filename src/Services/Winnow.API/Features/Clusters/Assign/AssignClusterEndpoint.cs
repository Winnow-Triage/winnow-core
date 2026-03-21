using System.Security.Claims;
using FastEndpoints;
using MediatR;

namespace Winnow.API.Features.Clusters.Assign;

public class AssignClusterRequest : Winnow.API.Features.Shared.ProjectScopedRequest
{
    public Guid Id { get; set; }
    public string? AssignedTo { get; set; }
}

public sealed class AssignClusterEndpoint(IMediator mediator) : Winnow.API.Features.Shared.ProjectScopedEndpoint<AssignClusterRequest, EmptyResponse>
{
    public override void Configure()
    {
        Post("/clusters/{id}/assign");
        Options(x => x.RequireAuthorization());
        Summary(s =>
        {
            s.Summary = "Assign a cluster";
            s.Description = "Assigns a cluster to a specific user.";
        });
    }

    public override async Task HandleAsync(AssignClusterRequest req, CancellationToken ct)
    {
        var command = new AssignClusterCommand(req.CurrentOrganizationId, req.Id, req.CurrentProjectId, req.AssignedTo);
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

        await Send.OkAsync(new EmptyResponse(), ct);
    }
}
