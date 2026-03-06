using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Infrastructure.MultiTenancy;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Billing;

public class BillingStatusResponse
{
    public string SubscriptionTier { get; init; } = default!;
    public int ReportsUsedThisMonth { get; init; }
    public int? ReportLimit { get; init; }
    public int? MonthlySummaryLimit { get; init; }
    public int CurrentMonthSummaries { get; init; }
    public bool HasActiveSubscription { get; init; }
}

public sealed class GetBillingStatusEndpoint(WinnowDbContext db, ITenantContext tenantContext) : EndpointWithoutRequest<BillingStatusResponse>
{
    public override void Configure()
    {
        Get("/billing/status");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!tenantContext.CurrentOrganizationId.HasValue)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var orgId = tenantContext.CurrentOrganizationId.Value;

        var org = await db.Organizations
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == orgId, ct);

        if (org == null)
        {
            await Send.NotFoundAsync(cancellation: ct);
            return;
        }

        var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var reportCount = await db.Reports
            .Where(r => r.OrganizationId == orgId && r.CreatedAt >= startOfMonth)
            .CountAsync(ct);

        int? limit = org.SubscriptionTier?.ToLowerInvariant() switch
        {
            "free" => 50,
            "starter" => 500,
            "pro" => null,
            "enterprise" => null,
            _ => 50
        };

        var effectiveLimit = org.MonthlySummaryLimit;
        if (effectiveLimit == 0)
        {
            effectiveLimit = org.SubscriptionTier?.ToLowerInvariant() switch
            {
                "enterprise" => -1,
                "pro" => 500,
                "starter" => 50,
                _ => 0
            };
        }

        int? aiLimit = effectiveLimit == -1 ? null : effectiveLimit;

        var response = new BillingStatusResponse
        {
            SubscriptionTier = org.SubscriptionTier ?? "Free",
            ReportsUsedThisMonth = reportCount,
            ReportLimit = limit,
            MonthlySummaryLimit = aiLimit,
            CurrentMonthSummaries = org.CurrentMonthSummaries,
            HasActiveSubscription = !string.IsNullOrEmpty(org.StripeSubscriptionId) && !string.Equals(org.SubscriptionTier, "Free", StringComparison.OrdinalIgnoreCase)
        };

        await Send.OkAsync(response, cancellation: ct);
    }
}
