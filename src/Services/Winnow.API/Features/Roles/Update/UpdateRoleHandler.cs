using MediatR;
using Microsoft.EntityFrameworkCore;
using Winnow.API.Domain.Security;
using Winnow.API.Infrastructure.Persistence;
using Winnow.API.Infrastructure.Security.Authorization;
using Winnow.API.Features.Shared;

namespace Winnow.API.Features.Roles.Update;

[RequirePermission("roles:manage")]
public record UpdateRoleCommand(Guid CurrentOrganizationId, Guid RoleId, string Name, List<Guid> PermissionIds) : IRequest<UpdateRoleResult>, IOrgScopedRequest;

public record UpdateRoleResponse(Guid Id, string Name);

public record UpdateRoleResult(bool IsSuccess, UpdateRoleResponse? Data = null, string? ErrorMessage = null, int? StatusCode = null);

public class UpdateRoleHandler(WinnowDbContext db) : IRequestHandler<UpdateRoleCommand, UpdateRoleResult>
{
    public async Task<UpdateRoleResult> Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await db.Roles
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.Id == request.RoleId && r.OrganizationId == request.CurrentOrganizationId, cancellationToken);

        if (role == null)
        {
            return new UpdateRoleResult(false, null, "Role not found or is a system role that cannot be edited", 404);
        }

        if (role.Name != request.Name)
        {
            var existingName = await db.Roles
                .AnyAsync(r => r.Name == request.Name && r.OrganizationId == request.CurrentOrganizationId && r.Id != request.RoleId, cancellationToken);

            if (existingName)
            {
                return new UpdateRoleResult(false, null, "Role with this name already exists", 400);
            }
        }

        // Verify the given permission IDs exist
        var validPermissionsCount = await db.Permissions
            .Where(p => request.PermissionIds.Contains(p.Id))
            .CountAsync(cancellationToken);

        if (validPermissionsCount != request.PermissionIds.Count)
        {
            return new UpdateRoleResult(false, null, "One or more invalid permission IDs provided", 400);
        }

        // Update name
        role.GetType().GetProperty(nameof(role.Name))!.SetValue(role, request.Name);

        // Update permissions
        // Remove old ones not in the request
        var toRemove = role.Permissions.Where(rp => !request.PermissionIds.Contains(rp.PermissionId)).ToList();
        foreach (var rp in toRemove)
        {
            db.RolePermissions.Remove(rp);
        }

        // Add new ones
        var existingPermIds = role.Permissions.Select(rp => rp.PermissionId).ToHashSet();
        foreach (var pId in request.PermissionIds)
        {
            if (!existingPermIds.Contains(pId))
            {
                db.RolePermissions.Add(new RolePermission(role.Id, pId));
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        return new UpdateRoleResult(true, new UpdateRoleResponse(role.Id, role.Name));
    }
}
