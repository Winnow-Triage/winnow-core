using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Infrastructure.MultiTenancy;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Teams;

public class UpdateTeamRequest
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed class UpdateTeamEndpoint(WinnowDbContext db, ITenantContext tenantContext)
    : Endpoint<UpdateTeamRequest, TeamResponse>
{
    public override void Configure()
    {
        Put("/teams/{id}");
        Summary(s =>
        {
            s.Summary = "Update a team";
            s.Description = "Modifies an existing team in the current organization.";
        });
    }

    public override async Task HandleAsync(UpdateTeamRequest req, CancellationToken ct)
    {
        if (!tenantContext.CurrentOrganizationId.HasValue)
        {
            ThrowError("No organization context.");
        }

        var team = await db.Teams
            .FirstOrDefaultAsync(t => t.Id == req.Id && t.OrganizationId == tenantContext.CurrentOrganizationId.Value, ct);

        if (team == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        if (string.IsNullOrWhiteSpace(req.Name))
        {
            ThrowError("Team name is required.");
        }

        team.Name = req.Name.Trim();
        await db.SaveChangesAsync(ct);

        await Send.OkAsync(new TeamResponse
        {
            Id = team.Id,
            Name = team.Name,
            CreatedAt = team.CreatedAt,
            ProjectCount = await db.Projects.CountAsync(p => p.TeamId == team.Id, ct),
            Members = await db.TeamMembers
                .Where(tm => tm.TeamId == team.Id)
                .Select(tm => new TeamMemberSummary
                {
                    UserId = tm.UserId,
                    FullName = tm.User!.FullName
                }).ToListAsync(ct)
        }, ct);
    }
}
