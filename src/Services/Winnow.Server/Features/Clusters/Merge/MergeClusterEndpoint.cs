using System.Security.Claims;
using FastEndpoints;
using MediatR;
using Winnow.Server.Features.Shared;

namespace Winnow.Server.Features.Clusters.Merge;

public class MergeClusterRequest : Winnow.Server.Features.Shared.ProjectScopedRequest
{
    /// <summary>The target cluster ID that others will be merged INTO.</summary>
    public Guid Id { get; set; }

    /// <summary>Source cluster IDs to merge into the target.</summary>
    public List<Guid> SourceIds { get; set; } = new();
}

public sealed class MergeClusterEndpoint(IMediator mediator)
    : Winnow.Server.Features.Shared.ProjectScopedEndpoint<MergeClusterRequest, ActionResponse>
{
    public override void Configure()
    {
        Post("/clusters/{Id}/merge");
        Summary(s =>
        {
            s.Summary = "Merge clusters";
            s.Description = "Merges multiple source clusters into a single target cluster. Reports in source clusters are re-assigned and marked as Duplicate. Empty source clusters are deleted.";
        });
        Options(x => x.RequireAuthorization());
    }

    public override async Task HandleAsync(MergeClusterRequest req, CancellationToken ct)
    {
        var command = new MergeClusterCommand(req.CurrentOrganizationId, req.Id, req.CurrentProjectId, req.SourceIds);
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

        await Send.OkAsync(new ActionResponse { Message = "Clusters merged successfully." }, ct);
    }
}
