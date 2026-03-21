using FastEndpoints;
using MediatR;
using Winnow.API.Features.Shared;

namespace Winnow.API.Features.Reports.Close;

/// <summary>
/// Request to close a cluster of reports.
/// </summary>
public class CloseClusterRequest : ProjectScopedRequest
{
    /// <summary>
    /// ID of the cluster to close.
    /// </summary>
    public Guid Id { get; set; }
}

public sealed class CloseClusterEndpoint(IMediator mediator) : ProjectScopedEndpoint<CloseClusterRequest, ActionResponse>
{
    public override void Configure()
    {
        Post("/clusters/{id}/close-cluster");
        Summary(s =>
        {
            s.Summary = "Close a cluster of reports";
            s.Description = "Closes all reports in the specified cluster.";
            s.Response<ActionResponse>(200, "Cluster closed successfully");
            s.Response(404, "Cluster not found");
        });
        Options(x => x.RequireAuthorization());
    }

    public override async Task HandleAsync(CloseClusterRequest req, CancellationToken ct)
    {
        var command = new CloseClusterCommand(req.CurrentOrganizationId, req.Id, req.CurrentProjectId);
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

        await Send.OkAsync(new ActionResponse { Message = result.Message! }, ct);
    }
}
