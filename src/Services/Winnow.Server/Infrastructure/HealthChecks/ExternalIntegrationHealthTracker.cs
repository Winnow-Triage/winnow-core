using System;
using System.Threading;

namespace Winnow.Server.Infrastructure.HealthChecks;

/// <summary>
/// Tracks the health state of the shared ExternalIntegrations HTTP client by recording
/// consecutive failures and the time of the last successful/failed call.
/// Used by <see cref="Winnow.Server.Infrastructure.HealthChecks.TenantIntegrationsHealthCheck"/> to surface circuit-breaker-like
/// status without needing direct access to Polly's internal state.
/// </summary>
public sealed class ExternalIntegrationHealthTracker
{
    private int _consecutiveFailures;
    private DateTimeOffset _lastSuccess = DateTimeOffset.UtcNow;
    private DateTimeOffset _lastFailure = DateTimeOffset.MinValue;
    private string _lastError = string.Empty;

    /// <summary>Number of consecutive failures since the last successful call.</summary>
    public int ConsecutiveFailures => _consecutiveFailures;

    /// <summary>Timestamp of the most recent successful request.</summary>
    public DateTimeOffset LastSuccess => _lastSuccess;

    /// <summary>Timestamp of the most recent failed request (MinValue if none).</summary>
    public DateTimeOffset LastFailure => _lastFailure;

    /// <summary>Message from the most recent failure, or empty string.</summary>
    public string LastError => _lastError;

    /// <summary>
    /// True when there have been 5+ consecutive failures — mirrors the default
    /// Polly standard resilience handler circuit-breaker threshold.
    /// </summary>
    public bool IsDegraded => _consecutiveFailures >= 5;

    public void RecordSuccess()
    {
        Interlocked.Exchange(ref _consecutiveFailures, 0);
        _lastSuccess = DateTimeOffset.UtcNow;
    }

    public void RecordFailure(string error)
    {
        Interlocked.Increment(ref _consecutiveFailures);
        _lastFailure = DateTimeOffset.UtcNow;
        _lastError = error;
    }
}
