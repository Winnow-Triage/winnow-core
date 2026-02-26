using Microsoft.EntityFrameworkCore;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Services.Quota;

/// <inheritdoc />
public class QuotaService(WinnowDbContext dbContext) : IQuotaService
{
    /// <inheritdoc />
    public async Task<(bool isOverage, bool isLocked)> GetIngestionQuotaStatusAsync(Guid organizationId, CancellationToken ct = default)
    {
        var org = await dbContext.Organizations
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == organizationId, ct);

        if (org == null)
        {
            return (true, true); // Lock if organization doesn't exist
        }

        // Tier definitions
        int baseLimit = org.SubscriptionTier switch
        {
            "Free" => 50,
            "Starter" => 500,
            "Pro" => int.MaxValue,
            "Enterprise" => int.MaxValue,
            _ => 50 // Default fallback
        };

        // Determine Grace Limit based on Tier
        int graceLimit = org.SubscriptionTier switch
        {
            "Free" => 100,
            "Starter" => 1000,
            "Pro" => int.MaxValue,
            "Enterprise" => int.MaxValue,
            _ => 100 // Default fallback
        };

        if (baseLimit == int.MaxValue)
        {
            return (false, false);
        }

        var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var reportCount = await dbContext.Reports
            .Where(r => r.OrganizationId == organizationId && r.CreatedAt >= startOfMonth)
            .CountAsync(ct);

        bool isOverage = reportCount >= baseLimit;
        bool isLocked = reportCount >= graceLimit;

        return (isOverage, isLocked);
    }

    /// <inheritdoc />
    public async Task EnforceRetroactiveRansomAsync(Guid organizationId, CancellationToken ct = default)
    {
        var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        await dbContext.Reports
            .Where(r => r.OrganizationId == organizationId
                     && r.CreatedAt >= startOfMonth
                     && r.IsOverage == true
                     && r.IsLocked == false)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.IsLocked, true), ct);
    }
}
