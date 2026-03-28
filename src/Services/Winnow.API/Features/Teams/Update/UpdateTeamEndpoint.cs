using Winnow.API.Features.Teams.List;
using FastEndpoints;
using MediatR;
using Winnow.API.Features.Shared;

namespace Winnow.API.Features.Teams.Update;

public class UpdateTeamRequest : OrganizationScopedRequest
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed class UpdateTeamEndpoint(IMediator mediator)
    : OrganizationScopedEndpoint<UpdateTeamRequest, TeamResponse>
{
    public override void Configure()
    {
        Put("/teams/{id}");
        Summary(s =>
        {
            s.Summary = "Update a team";
            s.Description = "Modifies an existing team in the current organization.";
        });
        Options(x => x.RequireAuthorization());
    }

    public override async Task HandleAsync(UpdateTeamRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
        {
            ThrowError("Team name is required.");
        }

        var command = new UpdateTeamCommand(req.CurrentOrganizationId, req.Id, req.Name);
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

        await Send.OkAsync(result.Data!, ct);
    }
}
