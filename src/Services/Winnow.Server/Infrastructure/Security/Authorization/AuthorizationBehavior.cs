using System.Reflection;
using System.Security.Claims;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Infrastructure.Persistence;
using Winnow.Server.Features.Shared;

namespace Winnow.Server.Infrastructure.Security.Authorization;

public class AuthorizationBehavior<TRequest, TResponse>(
    IHttpContextAccessor httpContextAccessor,
    WinnowDbContext dbContext)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requirePermissionAttributes = request.GetType().GetCustomAttributes<RequirePermissionAttribute>().ToList();

        if (requirePermissionAttributes.Count == 0)
        {
            return await next();
        }

        if (request is not IOrgScopedRequest orgScopedRequest)
        {
            throw new UnauthorizedAccessException($"Request {typeof(TRequest).Name} requires permissions but does not implement IOrgScopedRequest.");
        }

        var userId = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        var orgId = orgScopedRequest.CurrentOrganizationId;
        var requiredPermissions = requirePermissionAttributes.Select(a => a.Permission).ToList();

        var userPermissions = await dbContext.OrganizationMembers
            .Where(om => om.OrganizationId == orgId && om.UserId == userId && !om.IsLocked)
            .SelectMany(om => om.Role.Permissions)
            .Select(rp => rp.Permission.Name)
            .ToListAsync(cancellationToken);

        foreach (var requiredPermission in requiredPermissions)
        {
            if (!userPermissions.Contains(requiredPermission))
            {
                throw new UnauthorizedAccessException($"User does not have required permission '{requiredPermission}' for organization {orgId}.");
            }
        }

        return await next();
    }
}
