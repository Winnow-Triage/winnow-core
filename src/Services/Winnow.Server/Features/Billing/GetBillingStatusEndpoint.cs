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

        var response = new BillingStatusResponse
        {
            SubscriptionTier = org.Plan.Name,
            ReportsUsedThisMonth = org.ReportQuota.Consumed,
            ReportLimit = org.Plan.MonthlyReportLimit == int.MaxValue ? null : org.Plan.MonthlyReportLimit,
            CurrentMonthSummaries = org.SummaryQuota.Consumed,
            MonthlySummaryLimit = org.Plan.MonthlySummaryLimit == int.MaxValue ? null : org.Plan.MonthlySummaryLimit,
            HasActiveSubscription = org.HasActiveSubscription
        };

        await Send.OkAsync(response, cancellation: ct);
    }
}
