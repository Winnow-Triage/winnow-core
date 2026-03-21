using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Winnow.API.Infrastructure.HealthChecks;

/// <summary>
/// Background publisher that receives periodic HealthCheck results from .NET's built-in poller,
/// and saves them to the singleton CachedHealthReportService for instant endpoint retrieval.
/// </summary>
public class HealthReportPublisher(CachedHealthReportService cachedService) : IHealthCheckPublisher
{
    public Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
    {
        // Save the latest health report to the singleton cache
        cachedService.UpdateReport(report);
        return Task.CompletedTask;
    }
}
