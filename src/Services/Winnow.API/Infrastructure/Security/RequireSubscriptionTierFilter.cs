using Microsoft.EntityFrameworkCore;
using Winnow.API.Domain.Organizations.ValueObjects;
using Winnow.API.Infrastructure.Persistence;

namespace Winnow.API.Infrastructure.Security;

/// <summary>
/// A Minimal API Endpoint Filter that restricts access based on the Organization's subscription tier.
/// Performs a live database check to ensure recent billing updates take effect immediately without requiring a relogin.
/// </summary>
public class RequireSubscriptionTierFilter(SubscriptionPlan requiredTier) : IEndpointFilter
{
    private readonly SubscriptionPlan _requiredTier = requiredTier;

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var db = context.HttpContext.RequestServices.GetRequiredService<WinnowDbContext>();

        // Extract OrganizationId from the JWT Claims
        var orgIdClaim = context.HttpContext.User.FindFirst("OrganizationId")?.Value;

        if (string.IsNullOrEmpty(orgIdClaim) || !Guid.TryParse(orgIdClaim, out var orgId))
        {
            return Results.Forbid();
        }

        // Live check against the database so recent upgrades/downgrades take immediate effect
        var organization = await db.Organizations
            .AsNoTracking() // Prevent tracking overhead just for auth check
            .FirstOrDefaultAsync(o => o.Id == orgId);

        if (organization == null || !organization.Plan.IsAtLeast(_requiredTier))
        {
            return Results.Forbid();
        }

        return await next(context);
    }
}
