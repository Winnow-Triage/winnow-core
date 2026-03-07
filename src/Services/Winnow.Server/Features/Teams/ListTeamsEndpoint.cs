using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Infrastructure.MultiTenancy;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Teams;

public class TeamResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int ProjectCount { get; set; }
    public List<TeamMemberSummary> Members { get; set; } = [];
    public List<TeamProjectSummary> Projects { get; set; } = [];
}

public class TeamProjectSummary
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class TeamMemberSummary
{
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
}

public sealed class ListTeamsEndpoint(WinnowDbContext db, ITenantContext tenantContext)
    : EndpointWithoutRequest<List<TeamResponse>>
{
    public override void Configure()
    {
        Get("/teams");
        Summary(s =>
        {
            s.Summary = "List all teams in the current organization";
            s.Description = "Returns a list of all teams belonging to the active organization.";
        });
        Options(x => x.RequireAuthorization());
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!tenantContext.CurrentOrganizationId.HasValue)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var orgId = tenantContext.CurrentOrganizationId.Value;

        var teams = await db.Teams
            .AsNoTracking()
            .AsSplitQuery()
            .Where(t => t.OrganizationId == orgId)
            .OrderBy(t => t.Name)
            .Select(t => new TeamResponse
            {
                Id = t.Id,
                Name = t.Name,
                CreatedAt = t.CreatedAt,
                ProjectCount = t.Projects.Count,
                Members = db.TeamMembers
                    .Where(tm => tm.TeamId == t.Id)
                    .Select(tm => new TeamMemberSummary
                    {
                        UserId = tm.UserId,
                        FullName = tm.User!.FullName
                    }).ToList(),
                Projects = db.Projects
                    .Where(p => p.TeamId == t.Id)
                    .Select(p => new TeamProjectSummary { Id = p.Id, Name = p.Name })
                    .ToList()
            })
            .ToListAsync(ct);

        // FastEndpoints native syntax
        await Send.OkAsync(teams, ct);
    }
}