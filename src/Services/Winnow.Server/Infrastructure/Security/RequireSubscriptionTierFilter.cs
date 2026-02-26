using System.Security.Claims;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Infrastructure.Security;

/// <summary>
/// A Minimal API Endpoint Filter that restricts access based on the Organization's subscription tier.
/// Performs a live database check to ensure recent billing updates take effect immediately without requiring a relogin.
/// </summary>
public class RequireSubscriptionTierFilter(string requiredTier) : IEndpointFilter
{
    private readonly string _requiredTier = requiredTier;

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

        if (organization == null)
        {
            return Results.Forbid();
        }

        // Fallback to "Free" if the property is null for any reason
        var currentTier = organization.SubscriptionTier ?? "Free";

        if (!IsTierSufficient(currentTier, _requiredTier))
        {
            return Results.Forbid();
        }

        return await next(context);
    }

    private static bool IsTierSufficient(string currentTier, string requiredTier)
    {
        var tierLevels = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            { "Free", 0 },
            { "Starter", 1 },
            { "Pro", 2 },
            { "Enterprise", 3 }
        };

        var currentLevel = tierLevels.GetValueOrDefault(currentTier, 0);
        var requiredLevel = tierLevels.GetValueOrDefault(requiredTier, 0);

        return currentLevel >= requiredLevel;
    }
}
