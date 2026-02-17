namespace Winnow.Server.Features.Dashboard;

public interface IDashboardService
{
    Task<DashboardMetricsDto> GetDashboardMetricsAsync(Guid projectId, CancellationToken ct);
}
