using FastEndpoints;

namespace Winnow.Server.Features.Dashboard;

public class GetDashboardMetricsEndpoint(IDashboardService dashboardService) : EndpointWithoutRequest<DashboardMetricsDto>
{
    public override void Configure()
    {
        Get("/dashboard/metrics");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var metrics = await dashboardService.GetDashboardMetricsAsync(ct);
        await Send.OkAsync(metrics, ct);
    }
}
