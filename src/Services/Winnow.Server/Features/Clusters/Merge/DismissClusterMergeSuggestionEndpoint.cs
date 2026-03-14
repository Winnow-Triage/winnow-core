using System.Security.Claims;
using FastEndpoints;
using MediatR;
using Winnow.Server.Features.Shared;

namespace Winnow.Server.Features.Clusters.Merge;

public class DismissClusterMergeSuggestionRequest
{
    public Guid Id { get; set; }
}

public sealed class DismissClusterMergeSuggestionEndpoint(IMediator mediator)
    : Endpoint<DismissClusterMergeSuggestionRequest, ActionResponse>
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
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) ThrowError("Unauthorized", 401);

        if (!HttpContext.Request.Headers.TryGetValue("X-Project-ID", out var projectIdHeader))
        {
            ThrowError("Project ID is required in X-Project-ID header", 400);
        }

        if (!Guid.TryParse(projectIdHeader, out var projectId))
        {
            ThrowError("Invalid Project ID format", 400);
        }

        var tenantId = HttpContext.Items["TenantId"]?.ToString() ?? "default";

        var command = new DismissClusterMergeSuggestionCommand(req.Id, projectId, tenantId);
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
