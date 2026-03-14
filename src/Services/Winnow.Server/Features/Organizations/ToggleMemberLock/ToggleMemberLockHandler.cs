using MediatR;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Organizations.ToggleMemberLock;

public record ToggleMemberLockCommand(Guid OrganizationId, string MemberUserId, string CurrentUserId) : IRequest<ToggleMemberLockResult>;

public record ToggleMemberLockResult(bool IsSuccess, bool? IsLocked = null, string? ErrorMessage = null, int? StatusCode = null);

public class ToggleMemberLockHandler(WinnowDbContext db) : IRequestHandler<ToggleMemberLockCommand, ToggleMemberLockResult>
{
    public async Task<ToggleMemberLockResult> Handle(ToggleMemberLockCommand request, CancellationToken cancellationToken)
    {
        var isOwner = await db.OrganizationMembers
            .AnyAsync(om => om.OrganizationId == request.OrganizationId && om.UserId == request.CurrentUserId && (om.Role == "owner" || om.Role == "Admin"), cancellationToken);

        if (!isOwner)
        {
            return new ToggleMemberLockResult(false, null, "Forbidden", 403);
        }

        var member = await db.OrganizationMembers
            .FirstOrDefaultAsync(om => om.OrganizationId == request.OrganizationId && om.UserId == request.MemberUserId, cancellationToken);

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
