using Winnow.API.Features.Teams.List;
using FastEndpoints;
using MediatR;
using Winnow.API.Features.Shared;

namespace Winnow.API.Features.Teams.Create;

public class CreateTeamRequest : OrganizationScopedRequest
{
    public string Name { get; set; } = string.Empty;
}

public sealed class CreateTeamEndpoint(IMediator mediator)
    : OrganizationScopedEndpoint<CreateTeamRequest, TeamResponse>
{
    public override void Configure()
    {
        Post("/teams");
        Summary(s =>
        {
            s.Summary = "Create a new team";
            s.Description = "Adds a new team to the current organization.";
        });
        Options(x => x.RequireAuthorization());
    }

    public override async Task HandleAsync(CreateTeamRequest req, CancellationToken ct)
    {

        if (string.IsNullOrWhiteSpace(req.Name))
        {
            ThrowError("Team name is required.");
            return;
        }

        var command = new CreateTeamCommand(req.CurrentOrganizationId, req.Name);
        var result = await mediator.Send(command, ct);

        if (!result.IsSuccess)
        {
            ThrowError(result.ErrorMessage ?? "Internal Server Error", result.StatusCode ?? 500);
            return;
        }

        await Send.OkAsync(result.Data!, ct);
    }
}
