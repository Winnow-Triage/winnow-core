using MediatR;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Infrastructure.Persistence;
using Winnow.Server.Infrastructure.Security.Authorization;

namespace Winnow.Server.Features.Organizations.UpdateMember;

[RequirePermission("members:manage")]
public record UpdateMemberCommand(Guid OrgId, string UserId, Guid RoleId) : IRequest<UpdateMemberResult>, IOrgScopedRequest;

public record UpdateMemberResponse(string UserId, Guid RoleId);

public record UpdateMemberResult(bool IsSuccess, UpdateMemberResponse? Data = null, string? ErrorMessage = null, int? StatusCode = null);

public class UpdateMemberHandler(WinnowDbContext db) : IRequestHandler<UpdateMemberCommand, UpdateMemberResult>
{
    public async Task<UpdateMemberResult> Handle(UpdateMemberCommand request, CancellationToken cancellationToken)
    {
        var member = await db.OrganizationMembers
            .Include(m => m.Role)
            .FirstOrDefaultAsync(m => m.OrganizationId == request.OrgId && m.UserId == request.UserId, cancellationToken);

        if (member == null)
        {
            return new UpdateMemberResult(false, null, "Member not found", 404);
        }

        // Prevent modifying the Owner's role directly unless handling specific conditions
        // Usually there needs to be at least one owner.
        if (member.Role?.Name == "Owner")
        {
            var otherOwners = await db.OrganizationMembers
                .Where(m => m.OrganizationId == request.OrgId && m.UserId != request.UserId && m.Role.Name == "Owner" && !m.IsLocked)
                .CountAsync(cancellationToken);

            if (otherOwners == 0)
            {
                return new UpdateMemberResult(false, null, "Cannot change the role of the last active Owner.", 400);
            }
        }

        var newRole = await db.Roles.FirstOrDefaultAsync(r => r.Id == request.RoleId, cancellationToken);
        if (newRole == null || (newRole.OrganizationId != null && newRole.OrganizationId != request.OrgId))
        {
            return new UpdateMemberResult(false, null, "Invalid role.", 400);
        }

        member.ChangeRole(newRole.Id);


        await db.SaveChangesAsync(cancellationToken);

        return new UpdateMemberResult(true, new UpdateMemberResponse(request.UserId, request.RoleId));
    }
}
