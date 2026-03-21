using Microsoft.EntityFrameworkCore;
using Winnow.API.Infrastructure.Persistence;

namespace Winnow.API.Services.Quota;

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

        int baseLimit = org.Plan.MonthlyReportLimit;
        int graceLimit = org.Plan.MonthlyReportGraceLimit;

        // Pro and Enterprise use int.MaxValue
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

    /// <inheritdoc />
    public async Task ResolveQuotaDiscrepanciesAsync(Guid organizationId, CancellationToken ct = default)
    {
        var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var (isOverage, isLocked) = await GetIngestionQuotaStatusAsync(organizationId, ct);

        if (!isLocked)
        {
            // Unlock anything that shouldn't be locked
            await dbContext.Reports
                .Where(r => r.OrganizationId == organizationId
                         && r.CreatedAt >= startOfMonth
                         && r.IsLocked == true)
                .ExecuteUpdateAsync(s => s.SetProperty(r => r.IsLocked, false), ct);
        }
        else
        {
            // Lock everything if they downgraded and are currently over the grace limit
            await dbContext.Reports
                .Where(r => r.OrganizationId == organizationId
                         && r.CreatedAt >= startOfMonth
                         && r.IsLocked == false)
                .ExecuteUpdateAsync(s => s.SetProperty(r => r.IsLocked, true), ct);
        }

        if (!isOverage)
        {
            // Reset overage if their plan now covers all the reports
            await dbContext.Reports
                .Where(r => r.OrganizationId == organizationId
                         && r.CreatedAt >= startOfMonth
                         && r.IsOverage == true)
                .ExecuteUpdateAsync(s => s.SetProperty(r => r.IsOverage, false), ct);
        }
    }
}