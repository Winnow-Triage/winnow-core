using FastEndpoints;
using MediatR;
using Winnow.API.Features.Shared;

namespace Winnow.API.Features.Teams.Delete;

public class DeleteTeamRequest : OrganizationScopedRequest
{
    public Guid Id { get; set; }
}

public sealed class DeleteTeamEndpoint(IMediator mediator) : OrganizationScopedEndpoint<DeleteTeamRequest>
{
    public override void Configure()
    {
        Delete("/teams/{id}");
        Summary(s =>
        {
            s.Summary = "Delete a team";
            s.Description = "Permanently removes a team and unassigns its projects (projects are NOT deleted).";
        });
        Options(x => x.RequireAuthorization());
    }

    public override async Task HandleAsync(DeleteTeamRequest req, CancellationToken ct)
    {
        var command = new DeleteTeamCommand(req.CurrentOrganizationId, req.Id);
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

        await Send.NoContentAsync(cancellation: ct);
    }
}
