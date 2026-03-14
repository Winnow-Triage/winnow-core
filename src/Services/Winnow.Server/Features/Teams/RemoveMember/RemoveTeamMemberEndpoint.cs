using FastEndpoints;
using MediatR;
using Winnow.Server.Features.Shared;

namespace Winnow.Server.Features.Teams.RemoveMember;

public class RemoveTeamMemberRequest : OrganizationScopedRequest
{
    public Guid TeamId { get; set; }
    public string UserId { get; set; } = string.Empty;
}

public sealed class RemoveTeamMemberEndpoint(IMediator mediator)
    : OrganizationScopedEndpoint<RemoveTeamMemberRequest>
{
    public override void Configure()
    {
        Delete("/teams/{teamId}/members/{userId}");
        Summary(s =>
        {
            s.Summary = "Remove a member from a team";
            s.Description = "Dissociates a user from a specific team.";
        });
        Options(x => x.RequireAuthorization());
    }

    public override async Task HandleAsync(RemoveTeamMemberRequest req, CancellationToken ct)
    {
        var command = new RemoveTeamMemberCommand(req.TeamId, req.UserId, req.CurrentOrganizationId);
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
