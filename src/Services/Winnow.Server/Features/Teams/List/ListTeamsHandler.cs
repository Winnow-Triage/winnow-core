using MediatR;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Infrastructure.Persistence;

using Winnow.Server.Infrastructure.Security.Authorization;

namespace Winnow.Server.Features.Teams.List;

[RequirePermission("teams:read")]
public record ListTeamsQuery(Guid OrgId) : IRequest<ListTeamsResult>, IOrgScopedRequest;

public record ListTeamsResult(bool IsSuccess, List<TeamResponse>? Data = null, string? ErrorMessage = null, int? StatusCode = null);

public class ListTeamsHandler(WinnowDbContext db) : IRequestHandler<ListTeamsQuery, ListTeamsResult>
{
    public async Task<ListTeamsResult> Handle(ListTeamsQuery request, CancellationToken cancellationToken)
    {
        var teams = await db.Teams
            .AsNoTracking()
            .AsSplitQuery()
            .Where(t => t.OrganizationId == request.OrgId)
            .OrderBy(t => t.Name)
            .Select(t => new TeamResponse
            {
                Id = t.Id,
                Name = t.Name,
                CreatedAt = t.CreatedAt,
                ProjectCount = t.Projects.Count,
                Members = db.TeamMembers
                    .Where(tm => tm.TeamId == t.Id)
                    .Join(db.Users, tm => tm.UserId, u => u.Id, (tm, u) => new TeamMemberSummary
                    {
                        UserId = tm.UserId,
                        FullName = u.FullName
                    }).ToList(),
                Projects = db.Projects
                    .Where(p => p.TeamId == t.Id)
                    .Select(p => new TeamProjectSummary { Id = p.Id, Name = p.Name })
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        return new ListTeamsResult(true, teams);
    }
}
