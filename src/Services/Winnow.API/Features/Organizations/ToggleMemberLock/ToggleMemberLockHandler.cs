using MediatR;
using Winnow.API.Infrastructure.Security.Authorization;
using Microsoft.EntityFrameworkCore;
using Winnow.API.Infrastructure.Persistence;
using Winnow.API.Features.Shared;

namespace Winnow.API.Features.Organizations.ToggleMemberLock;

[RequirePermission("members:manage")]
public record ToggleMemberLockCommand(Guid CurrentOrganizationId, string MemberUserId, string CurrentUserId) : IRequest<ToggleMemberLockResult>, IOrgScopedRequest;

public record ToggleMemberLockResult(bool IsSuccess, bool? IsLocked = null, string? ErrorMessage = null, int? StatusCode = null);

public class ToggleMemberLockHandler(WinnowDbContext db) : IRequestHandler<ToggleMemberLockCommand, ToggleMemberLockResult>
{
    public async Task<ToggleMemberLockResult> Handle(ToggleMemberLockCommand request, CancellationToken cancellationToken)
    {
        var isOwner = await db.OrganizationMembers
            .AnyAsync(om => om.OrganizationId == request.CurrentOrganizationId && om.UserId == request.CurrentUserId && (om.Role.Name == "Owner" || om.Role.Name == "Admin"), cancellationToken);

        if (!isOwner)
        {
            return new ToggleMemberLockResult(false, null, "Forbidden", 403);
        }

        var member = await db.OrganizationMembers
            .FirstOrDefaultAsync(om => om.OrganizationId == request.CurrentOrganizationId && om.UserId == request.MemberUserId, cancellationToken);

        if (member == null)
        {
            return new ToggleMemberLockResult(false, null, "Member not found", 404);
        }

        if (member.IsLocked)
            member.Unlock();
        else
            member.Lock();

        await db.SaveChangesAsync(cancellationToken);

        return new ToggleMemberLockResult(true, member.IsLocked);
    }
}
