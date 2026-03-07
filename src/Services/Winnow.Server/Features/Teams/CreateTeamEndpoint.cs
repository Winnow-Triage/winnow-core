using FastEndpoints;
using Winnow.Server.Domain.Teams;
using Winnow.Server.Features.Shared;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Teams;

public class CreateTeamRequest : OrganizationScopedRequest
{
    public string Name { get; set; } = string.Empty;
}

public sealed class CreateTeamEndpoint(WinnowDbContext db)
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
        }

        var team = new Team(req.CurrentOrganizationId, req.Name);

        db.Teams.Add(team);
        await db.SaveChangesAsync(ct);

        await Send.OkAsync(new TeamResponse
        {
            Id = team.Id,
            Name = team.Name,
            CreatedAt = team.CreatedAt,
            ProjectCount = 0,
            Members = []
        }, ct);
    }
}
