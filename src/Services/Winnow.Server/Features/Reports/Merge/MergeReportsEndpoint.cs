using FastEndpoints;
using MediatR;
using Winnow.Server.Features.Shared;

namespace Winnow.Server.Features.Reports.Merge;

/// <summary>
/// Request to merge multiple reports into a target report's cluster.
/// </summary>
public class MergeReportsRequest : ProjectScopedRequest
{
    /// <summary>
    /// The target report ID that others will be merged INTO.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// List of source report IDs to merge into the target.
    /// </summary>
    public List<Guid> SourceIds { get; set; } = [];
}

public sealed class MergeReportsEndpoint(IMediator mediator) : ProjectScopedEndpoint<MergeReportsRequest, ActionResponse>
{
    public override void Configure()
    {
        Post("/reports/{Id}/merge");
        Summary(s =>
        {
            s.Summary = "Merge reports into a cluster";
            s.Description = "Merges multiple source reports into the cluster of a single target report. Source reports are marked as Duplicate.";
            s.Response<ActionResponse>(200, "Reports merged successfully");
            s.Response(404, "Target report not found");
        });
        Options(x => x.RequireAuthorization());
    }

    public override async Task HandleAsync(MergeReportsRequest req, CancellationToken ct)
    {
        var command = new MergeReportsCommand(req.Id, req.CurrentProjectId, req.CurrentOrganizationId, req.SourceIds);
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
