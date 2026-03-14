using FastEndpoints;
using MediatR;
using Winnow.Server.Features.Shared;

namespace Winnow.Server.Features.Reports.SuggestActions;

/// <summary>
/// Request to dismiss a suggested cluster assignment.
/// </summary>
public class DismissSuggestionRequest : ProjectScopedRequest
{
    /// <summary>
    /// ID of the report to dismiss the suggestion for.
    /// </summary>
    public Guid Id { get; set; }
}

public sealed class DismissSuggestionEndpoint(IMediator mediator) : ProjectScopedEndpoint<DismissSuggestionRequest, ActionResponse>
{
    public override void Configure()
    {
        Post("/reports/{id}/dismiss-suggestion");
        Summary(s =>
        {
            s.Summary = "Dismiss a suggested cluster assignment";
            s.Description = "Dismisses the AI-suggested cluster for the specified report and records a negative match.";
            s.Response<ActionResponse>(200, "Suggestion dismissed");
            s.Response(400, "No pending suggestion");
            s.Response(404, "Report not found");
        });
        Options(x => x.RequireAuthorization());
    }

    public override async Task HandleAsync(DismissSuggestionRequest req, CancellationToken ct)
    {
        var command = new DismissSuggestionCommand(req.Id, req.CurrentProjectId, req.CurrentOrganizationId);
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
