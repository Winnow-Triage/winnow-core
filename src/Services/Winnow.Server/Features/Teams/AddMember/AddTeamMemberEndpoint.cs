using FastEndpoints;
using MediatR;
using Winnow.Server.Features.Shared;

namespace Winnow.Server.Features.Teams.AddMember;

public class AddTeamMemberRequest : OrganizationScopedRequest
{
    public Guid TeamId { get; set; }
    public string UserId { get; set; } = string.Empty;
}

public sealed class AddTeamMemberEndpoint(IMediator mediator)
    : OrganizationScopedEndpoint<AddTeamMemberRequest>
{
    public override void Configure()
    {
        Post("/teams/{teamId}/members");
        Summary(s =>
        {
            s.Summary = "Add a member to a team";
            s.Description = "Assigns an organization member to a specific team.";
        });
        Options(x => x.RequireAuthorization());
    }

    public override async Task HandleAsync(AddTeamMemberRequest req, CancellationToken ct)
    {
        var command = new AddTeamMemberCommand(req.TeamId, req.UserId);
        var result = await mediator.Send(command, ct);

        if (!result.IsSuccess)
        {
            ThrowError(result.ErrorMessage ?? "Internal Server Error", result.StatusCode ?? 500);
            return;
        }

        await Send.NoContentAsync(cancellation: ct);
    }
}
