using System.Security.Claims;
using FastEndpoints;
using MediatR;

namespace Winnow.Server.Features.Clusters.Export;

public class ExportClusterRequest
{
    public Guid ClusterId { get; set; }

    [FromHeader("X-Project-ID")]
    public Guid ProjectId { get; set; }

    public Guid ConfigId { get; set; }
}

public class ExportClusterResponse
{
    public Uri ExternalUrl { get; set; } = default!;
}

public sealed class ExportClusterEndpoint(IMediator mediator)
    : Endpoint<ExportClusterRequest, ExportClusterResponse>
{
    public override void Configure()
    {
        Post("/clusters/{ClusterId}/export");
        Options(x => x.RequireAuthorization());
        Summary(s =>
        {
            s.Summary = "Export a cluster";
            s.Description = "Exports a cluster summary to an external system (e.g., Jira, Linear).";
        });
    }

    public override async Task HandleAsync(ExportClusterRequest req, CancellationToken ct)
    {
        // 1. Validate ProjectId was actually provided
        if (req.ProjectId == Guid.Empty)
        {
            ThrowError("Valid Project ID is required in X-Project-ID header", 400);
            return;
        }

        var command = new ExportClusterCommand(req.ClusterId, req.ProjectId, req.ConfigId);
        var result = await mediator.Send(command, ct);

        if (!result.IsSuccess)
        {
            if (result.StatusCode == 404)
            {
                await Send.NotFoundAsync(ct);
                return;
            }

            AddError($"Export failed: {result.ErrorMessage}");
            ThrowIfAnyErrors();
            return;
        }

        await Send.OkAsync(new ExportClusterResponse { ExternalUrl = result.ExternalUrl! }, ct);
    }
}
