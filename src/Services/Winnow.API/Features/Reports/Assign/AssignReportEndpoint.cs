using FastEndpoints;
using MediatR;
using Winnow.API.Features.Shared;

namespace Winnow.API.Features.Reports.Assign;

/// <summary>
/// Request to assign a report to a user.
/// </summary>
public class AssignReportRequest : ProjectScopedRequest
{
    /// <summary>
    /// ID of the report to assign.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Username or ID of the assignee. Set to null to unassign.
    /// </summary>
    public string? AssignedTo { get; set; }
}

public sealed class AssignReportEndpoint(IMediator mediator) : ProjectScopedEndpoint<AssignReportRequest, ActionResponse>
{
    public override void Configure()
    {
        Post("/reports/{id}/assign");
        Summary(s =>
        {
            s.Summary = "Assign a report";
            s.Description = "Assigns a report to a user. If the report was New, status changes to In Progress.";
            s.Response<ActionResponse>(200, "Report assigned successfully");
            s.Response(404, "Report not found");
        });
        Options(x => x.RequireAuthorization());
    }

    public override async Task HandleAsync(AssignReportRequest req, CancellationToken ct)
    {
        var command = new AssignReportCommand(req.CurrentOrganizationId, req.Id, req.CurrentProjectId, req.AssignedTo);
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
