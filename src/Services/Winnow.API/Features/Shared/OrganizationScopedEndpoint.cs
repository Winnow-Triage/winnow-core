using System.Security.Claims;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.API.Infrastructure.MultiTenancy;
using Winnow.API.Infrastructure.Persistence;

namespace Winnow.API.Features.Shared;

public abstract class OrganizationScopedEndpoint<TRequest, TResponse> : Endpoint<TRequest, TResponse>
    where TRequest : OrganizationScopedRequest, new()
    where TResponse : notnull
{
    public override async Task OnBeforeHandleAsync(TRequest req, CancellationToken ct)
    {
        var tenantContext = Resolve<ITenantContext>();

        if (!tenantContext.CurrentOrganizationId.HasValue)
        {
            ThrowError("No organization context provided.", 400);
        }

        req.CurrentOrganizationId = tenantContext.CurrentOrganizationId.Value;

        // Resolve the DbContext
        var db = Resolve<WinnowDbContext>();

        // Fetch membership and roles
        var membership = await db.OrganizationMembers
            .AsNoTracking()
            .Select(om => new { om.OrganizationId, om.UserId, Role = om.Role.Name })
            .FirstOrDefaultAsync(om => om.OrganizationId == req.CurrentOrganizationId && om.UserId == req.CurrentUserId, ct);

        if (membership == null)
        {
            // We return 404 instead of 403 to prevent attackers from "guessing" 
            // if an Organization ID exists by seeing a different error code.
            ThrowError("Organization not found or access denied.", 404);
        }

        // Populate the HashSet!
        if (!string.IsNullOrEmpty(membership.Role))
        {
            req.CurrentUserRoles.Add(membership.Role);
        }

        // Also add identity roles from JWT claims (e.g., "Admin", "SuperAdmin")
        foreach (var roleClaim in User.FindAll(ClaimTypes.Role))
        {
            req.CurrentUserRoles.Add(roleClaim.Value);
        }
    }
}

public abstract class OrganizationScopedEndpoint<TRequest> : Endpoint<TRequest>
    where TRequest : OrganizationScopedRequest, new()
{
    public override async Task OnBeforeHandleAsync(TRequest req, CancellationToken ct)
    {
        var tenantContext = Resolve<ITenantContext>();

        if (!tenantContext.CurrentOrganizationId.HasValue)
        {
            ThrowError("No organization context provided.", 400);
        }

        req.CurrentOrganizationId = tenantContext.CurrentOrganizationId.Value;

        // Resolve the DbContext
        var db = Resolve<WinnowDbContext>();

        // Fetch membership and roles
        var membership = await db.OrganizationMembers
            .AsNoTracking()
            .Select(om => new { om.OrganizationId, om.UserId, Role = om.Role.Name })
            .FirstOrDefaultAsync(om => om.OrganizationId == req.CurrentOrganizationId && om.UserId == req.CurrentUserId, ct);

        if (membership == null)
        {
            // We return 404 instead of 403 to prevent attackers from "guessing" 
            // if an Organization ID exists by seeing a different error code.
            ThrowError("Organization not found or access denied.", 404);
        }

        // Populate the HashSet!
        if (!string.IsNullOrEmpty(membership.Role))
        {
            req.CurrentUserRoles.Add(membership.Role);
        }
    }
}