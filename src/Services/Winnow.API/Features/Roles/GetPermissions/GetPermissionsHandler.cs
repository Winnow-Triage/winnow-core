using MediatR;
using Microsoft.EntityFrameworkCore;
using Winnow.API.Infrastructure.Persistence;
using Winnow.API.Infrastructure.Security.Authorization;
using Winnow.API.Features.Shared;

namespace Winnow.API.Features.Roles.GetPermissions;

[RequirePermission("roles:manage")]
public record GetPermissionsQuery(Guid CurrentOrganizationId) : IRequest<GetPermissionsResult>, IOrgScopedRequest;

public record PermissionDto(Guid Id, string Name, string? Description);

public record GetPermissionsResponse(List<PermissionDto> Permissions);

public record GetPermissionsResult(bool IsSuccess, GetPermissionsResponse? Data = null, string? ErrorMessage = null, int? StatusCode = null);

public class GetPermissionsHandler(WinnowDbContext db) : IRequestHandler<GetPermissionsQuery, GetPermissionsResult>
{
    public async Task<GetPermissionsResult> Handle(GetPermissionsQuery request, CancellationToken cancellationToken)
    {
        var permissions = await db.Permissions
            .AsNoTracking()
            .Select(p => new PermissionDto(p.Id, p.Name, p.Description))
            .ToListAsync(cancellationToken);

        // Permissions are global available resources, so we just return them.
        // Even though it's an IOrgScopedRequest, the authorization pipeline validates the user is in the org.
        return new GetPermissionsResult(true, new GetPermissionsResponse(permissions));
    }
}
