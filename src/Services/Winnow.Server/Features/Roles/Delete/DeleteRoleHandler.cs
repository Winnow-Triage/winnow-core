using MediatR;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Infrastructure.Persistence;
using Winnow.Server.Infrastructure.Security.Authorization;
using Winnow.Server.Features.Shared;

namespace Winnow.Server.Features.Roles.Delete;

[RequirePermission("roles:manage")]
public record DeleteRoleCommand(Guid OrganizationId, Guid RoleId) : IRequest<DeleteRoleResult>, IOrgScopedRequest
{
    public Guid OrgId => OrganizationId;
}

public record DeleteRoleResult(bool IsSuccess, string? ErrorMessage = null, int? StatusCode = null);

public class DeleteRoleHandler(WinnowDbContext db) : IRequestHandler<DeleteRoleCommand, DeleteRoleResult>
{
    public async Task<DeleteRoleResult> Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await db.Roles
            .Include(r => r.OrganizationMembers)
            .FirstOrDefaultAsync(r => r.Id == request.RoleId && r.OrganizationId == request.OrganizationId, cancellationToken);

        if (role == null)
        {
            return new DeleteRoleResult(false, "Role not found or is a system role that cannot be deleted", 404);
        }

        if (role.OrganizationMembers.Count != 0)
        {
            return new DeleteRoleResult(false, "Cannot delete a role that is currently assigned to members", 400);
        }

        db.Roles.Remove(role);
        await db.SaveChangesAsync(cancellationToken);

        return new DeleteRoleResult(true);
    }
}
