using FastEndpoints;
using Winnow.Server.Entities;
using Winnow.Server.Infrastructure.MultiTenancy;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Teams;

public class CreateTeamRequest
{
    public string Name { get; set; } = string.Empty;
}

public sealed class CreateTeamEndpoint(WinnowDbContext db, ITenantContext tenantContext)
    : Endpoint<CreateTeamRequest, TeamResponse>
{
    public override void Configure()
    {
        Post("/teams");
        Summary(s =>
        {
            s.Summary = "Create a new team";
            s.Description = "Adds a new team to the current organization.";
        });
    }

    public override async Task HandleAsync(CreateTeamRequest req, CancellationToken ct)
    {
        if (!tenantContext.CurrentOrganizationId.HasValue)
        {
            ThrowError("No organization context.");
        }

        if (string.IsNullOrWhiteSpace(req.Name))
        {
            ThrowError("Team name is required.");
        }

        var team = new Team
        {
            Id = Guid.NewGuid(),
            Name = req.Name.Trim(),
            OrganizationId = tenantContext.CurrentOrganizationId.Value,
            CreatedAt = DateTime.UtcNow
        };

        db.Teams.Add(team);
        await db.SaveChangesAsync(ct);

        await Send.OkAsync(new TeamResponse
        {
            Id = team.Id,
            Name = team.Name,
            CreatedAt = team.CreatedAt,
            ProjectCount = 0,
            Members = new()
        }, ct);
    }
}
