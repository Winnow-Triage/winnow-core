using MediatR;
using Winnow.API.Infrastructure.Security.Authorization;
using Microsoft.EntityFrameworkCore;
using Winnow.API.Infrastructure.Persistence;
using Winnow.API.Features.Shared;

namespace Winnow.API.Features.Billing.Get;

[RequirePermission("billing:manage")]
public record GetBillingStatusQuery(Guid CurrentOrganizationId) : IRequest<GetBillingStatusResult>, IOrgScopedRequest;

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
            .FirstOrDefaultAsync(o => o.Id == request.CurrentOrganizationId, cancellationToken);

        if (org == null)
        {
            return new GetBillingStatusResult(false, null, "Organization not found", 404);
        }

        var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var aiUsage = await db.AiUsages
            .AsNoTracking()
            .Where(u => u.OrganizationId == request.CurrentOrganizationId && u.CreatedAt >= startOfMonth)
            .GroupBy(u => new { u.ModelId, u.Provider })
            .Select(g => new AiUsageSummary
            {
                Model = g.Key.ModelId,
                Provider = g.Key.Provider,
                InputTokens = g.Sum(u => u.PromptTokens),
                OutputTokens = g.Sum(u => u.CompletionTokens),
                CallCount = g.Count()
            })
            .ToListAsync(cancellationToken);

        var response = new BillingStatusResponse
        {
            SubscriptionTier = org.Plan.Name,
            ReportsUsedThisMonth = org.ReportQuota.Consumed,
            ReportLimit = org.Plan.MonthlyReportLimit == int.MaxValue ? null : org.Plan.MonthlyReportLimit,
            CurrentMonthSummaries = org.SummaryQuota.Consumed,
            MonthlySummaryLimit = org.Plan.MonthlySummaryLimit == int.MaxValue ? null : org.Plan.MonthlySummaryLimit,
            HasActiveSubscription = org.HasActiveSubscription,
            MonthlyInputTokens = aiUsage.Sum(x => x.InputTokens),
            MonthlyOutputTokens = aiUsage.Sum(x => x.OutputTokens),
            AiUsageBreakdown = aiUsage
        };

        return new GetBillingStatusResult(true, response);
    }
}
