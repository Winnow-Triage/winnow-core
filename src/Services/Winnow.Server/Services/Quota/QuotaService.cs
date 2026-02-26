using Microsoft.EntityFrameworkCore;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Services.Quota;

/// <inheritdoc />
public class QuotaService(WinnowDbContext dbContext) : IQuotaService
{
    /// <inheritdoc />
    public async Task<bool> CanIngestReportAsync(Guid organizationId, CancellationToken ct = default)
    {
        var org = await dbContext.Organizations
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == organizationId, ct);

        if (org == null)
        {
            return false;
        }

        int monthlyLimit = org.SubscriptionTier switch
        {
            "Free" => 50,
            "Starter" => 500,
            "Pro" => int.MaxValue,
            "Enterprise" => int.MaxValue,
            _ => 50 // Default fallback
        };

        if (monthlyLimit == int.MaxValue)
        {
            return true;
        }

        var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var reportCount = await dbContext.Reports
            .Where(r => r.OrganizationId == organizationId && r.CreatedAt >= startOfMonth)
            .CountAsync(ct);

        return reportCount < monthlyLimit;
    }
}
