using MediatR;
using Microsoft.EntityFrameworkCore;
using Winnow.API.Features.Roles.GetPermissions;
using Winnow.API.Infrastructure.Persistence;
using Winnow.API.Infrastructure.Security.Authorization;
using Winnow.API.Features.Shared;

namespace Winnow.API.Features.Roles.List;

[RequirePermission("roles:manage")]
public record GetRolesQuery(Guid CurrentOrganizationId) : IRequest<GetRolesResult>, IOrgScopedRequest;

public record RoleDto(Guid Id, string Name, bool IsSystemRole, List<PermissionDto> Permissions);

public record GetRolesResponse(List<RoleDto> Roles);

public record GetRolesResult(bool IsSuccess, GetRolesResponse? Data = null, string? ErrorMessage = null, int? StatusCode = null);

public class GetRolesHandler(WinnowDbContext db) : IRequestHandler<GetRolesQuery, GetRolesResult>
{
    public async Task<GetRolesResult> Handle(GetRolesQuery request, CancellationToken cancellationToken)
    {
        // Fetch roles that are either system roles (OrganizationId == null) or belong to this org.
        var roles = await db.Roles
            .Include(r => r.Permissions)
                .ThenInclude(rp => rp.Permission)
            .Where(r => r.OrganizationId == null || r.OrganizationId == request.CurrentOrganizationId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var dtos = roles.Select(r => new RoleDto(
            r.Id,
            r.Name,
            r.OrganizationId == null,
            r.Permissions.Select(rp => new PermissionDto(rp.Permission.Id, rp.Permission.Name, rp.Permission.Description)).ToList()
        )).OrderBy(r => !r.IsSystemRole).ThenBy(r => r.Name).ToList();

        return new GetRolesResult(true, new GetRolesResponse(dtos));
    }
}
