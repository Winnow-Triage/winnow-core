using MediatR;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Teams.RemoveMember;

public record RemoveTeamMemberCommand(Guid TeamId, string UserId, Guid CurrentOrganizationId) : IRequest<RemoveTeamMemberResult>;

public record RemoveTeamMemberResult(bool IsSuccess, string? ErrorMessage = null, int? StatusCode = null);

public class RemoveTeamMemberHandler(WinnowDbContext db) : IRequestHandler<RemoveTeamMemberCommand, RemoveTeamMemberResult>
{
    public async Task<RemoveTeamMemberResult> Handle(RemoveTeamMemberCommand request, CancellationToken cancellationToken)
    {
        var member = await db.TeamMembers
            .Join(db.Teams, tm => tm.TeamId, t => t.Id, (tm, t) => new { tm, t })
            .Where(x => x.tm.TeamId == request.TeamId &&
                        x.tm.UserId == request.UserId &&
                        x.t.OrganizationId == request.CurrentOrganizationId)
            .Select(x => x.tm)
            .FirstOrDefaultAsync(cancellationToken);

        if (member == null)
        {
            return new RemoveTeamMemberResult(false, "Member not found", 404);
        }

        db.TeamMembers.Remove(member);
        await db.SaveChangesAsync(cancellationToken);

        return new RemoveTeamMemberResult(true);
    }
}
