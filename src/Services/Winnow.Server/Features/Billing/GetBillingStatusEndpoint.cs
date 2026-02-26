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

        int? limit = org.SubscriptionTier switch
        {
            "Free" => 50,
            "Starter" => 500,
            "Pro" => null,
            "Enterprise" => null,
            _ => 50
        };

        var response = new BillingStatusResponse
        {
            SubscriptionTier = org.SubscriptionTier,
            ReportsUsedThisMonth = reportCount,
            ReportLimit = limit,
            HasActiveSubscription = !string.IsNullOrEmpty(org.StripeSubscriptionId) && org.SubscriptionTier != "Free"
        };

        await Send.OkAsync(response, cancellation: ct);
    }
}
