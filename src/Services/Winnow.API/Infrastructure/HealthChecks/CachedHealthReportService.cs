using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Winnow.API.Infrastructure.HealthChecks;

/// <summary>
/// A singleton container that holds the most recent HealthReport from the background publisher.
/// This allows the /health/detailed endpoint to instantly return cached data without waiting 
/// for slow external integrations to time out.
/// </summary>
public class CachedHealthReportService
{
    private readonly object _lock = new();
    private HealthReport? _report;
    private DateTime? _lastUpdatedUtc;

    public HealthReport? Report
    {
        get
        {
            lock (_lock)
            {
                return _report;
            }
        }
    }

    public void UpdateReport(HealthReport report)
    {
        lock (_lock)
        {
            _report = report;
            _lastUpdatedUtc = DateTime.UtcNow;
        }
    }
}
