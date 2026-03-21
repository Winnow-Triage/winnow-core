using MediatR;
using Microsoft.EntityFrameworkCore;
using Winnow.API.Infrastructure.Persistence;

using Winnow.API.Infrastructure.Security.Authorization;
using Winnow.API.Features.Shared;

namespace Winnow.API.Features.Teams.Delete;

[RequirePermission("teams:write")]
public record DeleteTeamCommand(Guid CurrentOrganizationId, Guid Id) : IRequest<DeleteTeamResult>, IOrgScopedRequest;

public record DeleteTeamResult(bool IsSuccess, string? ErrorMessage = null, int? StatusCode = null);

public class DeleteTeamHandler(WinnowDbContext db) : IRequestHandler<DeleteTeamCommand, DeleteTeamResult>
{
    public async Task<DeleteTeamResult> Handle(DeleteTeamCommand request, CancellationToken cancellationToken)
    {
        var team = await db.Teams
            .FirstOrDefaultAsync(t => t.Id == request.Id && t.OrganizationId == request.CurrentOrganizationId, cancellationToken);

        if (team == null)
        {
            return new DeleteTeamResult(false, "Team not found", 404);
        }

        // Unassign projects (set TeamId to null)
        await db.Projects
            .Where(p => p.TeamId == team.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.TeamId, (Guid?)null), cancellationToken);

        // Remove team members
        await db.TeamMembers
            .Where(tm => tm.TeamId == team.Id)
            .ExecuteDeleteAsync(cancellationToken);

        db.Teams.Remove(team);
        await db.SaveChangesAsync(cancellationToken);

        return new DeleteTeamResult(true);
    }
}
