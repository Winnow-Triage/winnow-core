using MediatR;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Domain.Security;
using Winnow.Server.Infrastructure.Persistence;
using Winnow.Server.Infrastructure.Security.Authorization;
using Winnow.Server.Features.Shared;

namespace Winnow.Server.Features.Roles.Create;

[RequirePermission("roles:manage")]
public record CreateRoleCommand(Guid OrgId, string Name, List<Guid> PermissionIds) : IRequest<CreateRoleResult>, IOrgScopedRequest;

public record CreateRoleResponse(Guid Id, string Name);

public record CreateRoleResult(bool IsSuccess, CreateRoleResponse? Data = null, string? ErrorMessage = null, int? StatusCode = null);

public class CreateRoleHandler(WinnowDbContext db) : IRequestHandler<CreateRoleCommand, CreateRoleResult>
{
    public async Task<CreateRoleResult> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        var existingRole = await db.Roles
            .AnyAsync(r => r.Name == request.Name && r.OrganizationId == request.OrgId, cancellationToken);

        if (existingRole)
        {
            return new CreateRoleResult(false, null, "Role with this name already exists in the organization", 400);
        }

        // Verify the given permission IDs exist
        var validPermissionsCount = await db.Permissions
            .Where(p => request.PermissionIds.Contains(p.Id))
            .CountAsync(cancellationToken);

        if (validPermissionsCount != request.PermissionIds.Count)
        {
            return new CreateRoleResult(false, null, "One or more invalid permission IDs provided", 400);
        }

        var role = new Role(request.Name, request.OrgId);

        db.Roles.Add(role);

        foreach (var permissionId in request.PermissionIds)
        {
            db.RolePermissions.Add(new RolePermission(role.Id, permissionId));
        }

        await db.SaveChangesAsync(cancellationToken);

        return new CreateRoleResult(true, new CreateRoleResponse(role.Id, role.Name));
    }
}
