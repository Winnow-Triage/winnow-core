using FastEndpoints;
using MediatR;
using Winnow.Server.Features.Shared;

namespace Winnow.Server.Features.Reports.SuggestActions;

/// <summary>
/// Request to accept a suggested cluster assignment.
/// </summary>
public class AcceptSuggestionRequest : ProjectScopedRequest
{
    /// <summary>
    /// ID of the report to accept a suggestion for.
    /// </summary>
    public Guid Id { get; set; }
}

public sealed class AcceptSuggestionEndpoint(IMediator mediator) : ProjectScopedEndpoint<AcceptSuggestionRequest, ActionResponse>
{
    public override void Configure()
    {
        Post("/reports/{id}/accept-suggestion");
        Summary(s =>
        {
            s.Summary = "Accept a suggested cluster assignment";
            s.Description = "Accepts the AI-suggested cluster for the specified report, assigning it to the cluster.";
            s.Response<ActionResponse>(200, "Suggestion accepted");
            s.Response(400, "No pending suggestion");
            s.Response(404, "Report not found");
        });
        Options(x => x.RequireAuthorization());
    }

    public override async Task HandleAsync(AcceptSuggestionRequest req, CancellationToken ct)
    {
        var command = new AcceptSuggestionCommand(req.CurrentOrganizationId, req.Id, req.CurrentProjectId);
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
