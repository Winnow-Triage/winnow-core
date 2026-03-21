using MediatR;
using Winnow.API.Infrastructure.Security.Authorization;
using Microsoft.EntityFrameworkCore;
using Winnow.API.Infrastructure.Persistence;
using Winnow.API.Features.Shared;

namespace Winnow.API.Features.Organizations.RemoveMember;

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
