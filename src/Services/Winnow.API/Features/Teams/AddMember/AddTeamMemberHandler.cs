using MediatR;
using Microsoft.EntityFrameworkCore;
using Winnow.API.Infrastructure.Persistence;

using Winnow.API.Infrastructure.Security.Authorization;
using Winnow.API.Features.Shared;

namespace Winnow.API.Features.Teams.AddMember;

[RequirePermission("teams:write")]
public record AddTeamMemberCommand(Guid CurrentOrganizationId, Guid TeamId, string UserId) : IRequest<AddTeamMemberResult>, IOrgScopedRequest;

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

        var member = new Winnow.API.Domain.Teams.TeamMember(request.TeamId, request.UserId);

        db.TeamMembers.Add(member);
        await db.SaveChangesAsync(cancellationToken);

        return new AddTeamMemberResult(true);
    }
}
