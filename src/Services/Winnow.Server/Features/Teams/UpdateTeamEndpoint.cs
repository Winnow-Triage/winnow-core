using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Features.Shared;
using Winnow.Server.Infrastructure.MultiTenancy;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Teams;

public class UpdateTeamRequest : OrganizationScopedRequest
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed class UpdateTeamEndpoint(WinnowDbContext db)
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
        var team = await db.Teams
            .FirstOrDefaultAsync(t => t.Id == req.Id && t.OrganizationId == req.CurrentOrganizationId, ct);

        if (team == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        if (string.IsNullOrWhiteSpace(req.Name))
        {
            ThrowError("Team name is required.");
        }

        team.Rename(req.Name.Trim());
        await db.SaveChangesAsync(ct);

        await Send.OkAsync(new TeamResponse
        {
            Id = team.Id,
            Name = team.Name,
            CreatedAt = team.CreatedAt,
            ProjectCount = await db.Projects.CountAsync(p => p.TeamId == team.Id, ct),
            Members = await db.TeamMembers
                .Where(tm => tm.TeamId == team.Id)
                .Join(db.Users, tm => tm.UserId, u => u.Id, (tm, u) => new TeamMemberSummary
                {
                    UserId = tm.UserId,
                    FullName = u.FullName
                }).ToListAsync(ct),
            Projects = await db.Projects
                .Where(p => p.TeamId == team.Id)
                .Select(p => new TeamProjectSummary
                {
                    Id = p.Id,
                    Name = p.Name
                }).ToListAsync(ct)
        }, ct);
    }
}
