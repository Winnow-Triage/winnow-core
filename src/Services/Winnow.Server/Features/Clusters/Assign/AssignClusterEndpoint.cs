using System.Security.Claims;
using FastEndpoints;
using MediatR;

namespace Winnow.Server.Features.Clusters.Assign;

public class AssignClusterRequest
{
    public Guid Id { get; set; }
    public string? AssignedTo { get; set; }
}

public sealed class AssignClusterEndpoint(IMediator mediator) : Endpoint<AssignClusterRequest>
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
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) ThrowError("Unauthorized", 401);

        if (!HttpContext.Request.Headers.TryGetValue("X-Project-ID", out var projectIdHeader) ||
            !Guid.TryParse(projectIdHeader, out var projectId))
        {
            ThrowError("Valid Project ID is required in X-Project-ID header", 400);
            return;
        }

        var command = new AssignClusterCommand(req.Id, projectId, req.AssignedTo);
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

        await Send.NoContentAsync(ct);
    }
}
