using MediatR;
using Winnow.Server.Infrastructure.Security.Authorization;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Infrastructure.Persistence;
using Winnow.Server.Features.Shared;

namespace Winnow.Server.Features.Organizations.RemoveMember;

[RequirePermission("members:manage")]
public record RemoveOrganizationMemberCommand(Guid CurrentOrganizationId, string MemberUserId, string CurrentUserId) : IRequest<RemoveOrganizationMemberResult>, IOrgScopedRequest;

public record RemoveOrganizationMemberResult(bool IsSuccess, string? ErrorMessage = null, int? StatusCode = null);

public class RemoveOrganizationMemberHandler(WinnowDbContext db) : IRequestHandler<RemoveOrganizationMemberCommand, RemoveOrganizationMemberResult>
{
    public async Task<RemoveOrganizationMemberResult> Handle(RemoveOrganizationMemberCommand request, CancellationToken cancellationToken)
    {
        var isOwner = await db.OrganizationMembers
            .AnyAsync(om => om.OrganizationId == request.CurrentOrganizationId && om.UserId == request.CurrentUserId && (om.Role.Name == "Owner" || om.Role.Name == "Admin"), cancellationToken);

        if (!isOwner)
        {
            return new RemoveOrganizationMemberResult(false, "Forbidden", 403);
        }

        var member = await db.OrganizationMembers
            .FirstOrDefaultAsync(om => om.OrganizationId == request.CurrentOrganizationId && om.UserId == request.MemberUserId, cancellationToken);

        if (member == null)
        {
            return new RemoveOrganizationMemberResult(false, "Member not found", 404);
        }

        db.OrganizationMembers.Remove(member);
        await db.SaveChangesAsync(cancellationToken);

        return new RemoveOrganizationMemberResult(true);
    }
}
