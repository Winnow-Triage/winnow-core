using MediatR;
using Microsoft.EntityFrameworkCore;
using Winnow.API.Infrastructure.Persistence;

using Winnow.API.Infrastructure.Security.Authorization;
using Winnow.API.Features.Shared;

namespace Winnow.API.Features.Teams.List;

[RequirePermission("teams:read")]
public record ListTeamsQuery(Guid CurrentOrganizationId) : IRequest<ListTeamsResult>, IOrgScopedRequest;

public record ListTeamsResult(bool IsSuccess, List<TeamResponse>? Data = null, string? ErrorMessage = null, int? StatusCode = null);

public class ListTeamsHandler(WinnowDbContext db) : IRequestHandler<ListTeamsQuery, ListTeamsResult>
{
    public async Task<ListTeamsResult> Handle(ListTeamsQuery request, CancellationToken cancellationToken)
    {
        var teams = await db.Teams
            .AsNoTracking()
            .AsSplitQuery()
            .Where(t => t.OrganizationId == request.CurrentOrganizationId)
            .OrderBy(t => t.Name)
            .Select(t => new TeamResponse
            {
                Id = t.Id,
                Name = t.Name,
                CreatedAt = t.CreatedAt,
                ProjectCount = db.Projects.Count(p => p.TeamId == t.Id),
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
