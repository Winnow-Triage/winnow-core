using MediatR;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Billing.Get;

public record GetBillingStatusQuery(Guid OrganizationId) : IRequest<GetBillingStatusResult>;

public record GetBillingStatusResult(
    bool IsSuccess,
    BillingStatusResponse? Data = null,
    string? ErrorMessage = null,
    int? StatusCode = null);

public class GetBillingStatusHandler(WinnowDbContext db) : IRequestHandler<GetBillingStatusQuery, GetBillingStatusResult>
{
    public async Task<GetBillingStatusResult> Handle(GetBillingStatusQuery request, CancellationToken cancellationToken)
    {
        var org = await db.Organizations
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == request.OrganizationId, cancellationToken);

        if (org == null)
        {
            return new GetBillingStatusResult(false, null, "Organization not found", 404);
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

        return new GetBillingStatusResult(true, response);
    }
}
