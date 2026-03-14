using FastEndpoints;
using MediatR;
using Winnow.Server.Infrastructure.MultiTenancy;

namespace Winnow.Server.Features.Billing.Get;

public class BillingStatusResponse
{
    public string SubscriptionTier { get; init; } = default!;
    public int ReportsUsedThisMonth { get; init; }
    public int? ReportLimit { get; init; }
    public int? MonthlySummaryLimit { get; init; }
    public int CurrentMonthSummaries { get; init; }
    public bool HasActiveSubscription { get; init; }
}

public sealed class GetBillingStatusEndpoint(IMediator mediator, ITenantContext tenantContext) : EndpointWithoutRequest<BillingStatusResponse>
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

        var query = new GetBillingStatusQuery(tenantContext.CurrentOrganizationId.Value);
        var result = await mediator.Send(query, ct);

        if (!result.IsSuccess)
        {
            if (result.StatusCode == 404)
            {
                await Send.NotFoundAsync(cancellation: ct);
                return;
            }
            await Send.ErrorsAsync(400, cancellation: ct);
            return;
        }

        await Send.OkAsync(result.Data!, cancellation: ct);
    }
}
