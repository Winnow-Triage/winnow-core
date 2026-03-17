using Winnow.Server.Features.Teams.List;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Infrastructure.Persistence;

using Winnow.Server.Infrastructure.Security.Authorization;

namespace Winnow.Server.Features.Teams.Update;

[RequirePermission("teams:write")]
public record UpdateTeamCommand(Guid OrgId, Guid Id, string Name) : IRequest<UpdateTeamResult>, IOrgScopedRequest;

public record UpdateTeamResult(bool IsSuccess, TeamResponse? Data = null, string? ErrorMessage = null, int? StatusCode = null);

public class UpdateTeamHandler(WinnowDbContext db) : IRequestHandler<UpdateTeamCommand, UpdateTeamResult>
{
    public async Task<UpdateTeamResult> Handle(UpdateTeamCommand request, CancellationToken cancellationToken)
    {
        var team = await db.Teams
            .FirstOrDefaultAsync(t => t.Id == request.Id && t.OrganizationId == request.OrgId, cancellationToken);

        if (team == null)
        {
            return new UpdateTeamResult(false, null, "Team not found", 404);
        }

        team.Rename(request.Name.Trim());
        await db.SaveChangesAsync(cancellationToken);

        var data = new TeamResponse
        {
            Id = team.Id,
            Name = team.Name,
            CreatedAt = team.CreatedAt,
            ProjectCount = await db.Projects.CountAsync(p => p.TeamId == team.Id, cancellationToken),
            Members = await db.TeamMembers
                .Where(tm => tm.TeamId == team.Id)
                .Join(db.Users, tm => tm.UserId, u => u.Id, (tm, u) => new TeamMemberSummary
                {
                    UserId = tm.UserId,
                    FullName = u.FullName
                }).ToListAsync(cancellationToken),
            Projects = await db.Projects
                .Where(p => p.TeamId == team.Id)
                .Select(p => new TeamProjectSummary
                {
                    Id = p.Id,
                    Name = p.Name
                }).ToListAsync(cancellationToken)
        };

        return new UpdateTeamResult(true, data);
    }
}
