using FastEndpoints;
using MediatR;
using Winnow.Server.Features.Shared;

namespace Winnow.Server.Features.Reports.Ungroup;

/// <summary>
/// Request to remove a report from its cluster.
/// </summary>
public class UngroupReportRequest : ProjectScopedRequest
{
    /// <summary>
    /// ID of the report to ungroup.
    /// </summary>
    public Guid Id { get; set; }
}

public sealed class UngroupReportEndpoint(IMediator mediator) : ProjectScopedEndpoint<UngroupReportRequest, ActionResponse>
{
    public override void Configure()
    {
        Post("/reports/{id}/ungroup");
        Summary(s =>
        {
            s.Summary = "Ungroup a report";
            s.Description = "Removes a report from its current cluster and sets its status to New.";
            s.Response<ActionResponse>(200, "Report ungrouped successfully");
            s.Response(400, "Report is not grouped");
            s.Response(404, "Report not found");
        });
        Options(x => x.RequireAuthorization());
    }

    public override async Task HandleAsync(UngroupReportRequest req, CancellationToken ct)
    {
        var command = new UngroupReportCommand(req.CurrentOrganizationId, req.Id, req.CurrentProjectId);
        var result = await mediator.Send(command, ct);

        if (!result.IsSuccess)
        {
            if (result.StatusCode == 404)
            {
                await Send.NotFoundAsync(ct);
                return;
            }
            if (result.StatusCode == 400)
            {
                AddError(result.ErrorMessage!);
                ThrowIfAnyErrors();
            }
            ThrowError(result.ErrorMessage ?? "Internal Server Error", result.StatusCode ?? 500);
            return;
        }

        await Send.OkAsync(new ActionResponse { Message = result.Message! }, ct);
    }
}
