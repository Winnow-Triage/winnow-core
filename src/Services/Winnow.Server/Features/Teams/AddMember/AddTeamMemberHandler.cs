using MediatR;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Teams.AddMember;

public record AddTeamMemberCommand(Guid TeamId, string UserId) : IRequest<AddTeamMemberResult>;

public record AddTeamMemberResult(bool IsSuccess, string? ErrorMessage = null, int? StatusCode = null);

public class AddTeamMemberHandler(WinnowDbContext db) : IRequestHandler<AddTeamMemberCommand, AddTeamMemberResult>
{
    public async Task<AddTeamMemberResult> Handle(AddTeamMemberCommand request, CancellationToken cancellationToken)
    {
        var alreadyMember = await db.TeamMembers.AnyAsync(tm => tm.TeamId == request.TeamId && tm.UserId == request.UserId, cancellationToken);
        if (alreadyMember)
        {
            return new AddTeamMemberResult(true);
        }

        var member = new Winnow.Server.Domain.Teams.TeamMember(request.TeamId, request.UserId);

        db.TeamMembers.Add(member);
        await db.SaveChangesAsync(cancellationToken);

        return new AddTeamMemberResult(true);
    }
}
