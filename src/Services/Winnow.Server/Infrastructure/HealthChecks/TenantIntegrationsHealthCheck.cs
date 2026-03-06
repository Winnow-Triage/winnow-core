using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Infrastructure.HealthChecks;

/// <summary>
/// Reports the health of tenant-scoped integrations (GitHub, Jira, Trello).
///
/// Because each project stores its own credentials in the database there is no
/// single shared endpoint to probe. Instead this check:
///   1. Queries the DB for active integration counts grouped by provider.
///   2. Reads the shared <see cref="ExternalIntegrationHealthTracker"/> to surface
///      whether the ExternalIntegrations HTTP client has been experiencing failures
///      (mirrors the Polly circuit-breaker threshold of 5 consecutive errors).
/// </summary>
public class TenantIntegrationsHealthCheck : IHealthCheck
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ExternalIntegrationHealthTracker _tracker;

    public TenantIntegrationsHealthCheck(
        IServiceScopeFactory scopeFactory,
        ExternalIntegrationHealthTracker tracker)
    {
        _scopeFactory = scopeFactory;
        _tracker = tracker;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var data = new Dictionary<string, object>();

        // --- 1. Count active integrations by provider from any tenant's DB ---
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<WinnowDbContext>();

            var counts = await db.Integrations
                .AsNoTracking()
                .Where(i => i.IsActive)
                .GroupBy(i => i.Provider)
                .Select(g => new { Provider = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            if (counts.Count == 0)
            {
                data["ActiveIntegrations"] = "None configured";
            }
            else
            {
                foreach (var entry in counts)
                {
                    data[entry.Provider] = $"{entry.Count} active";
                }
            }
        }
        catch (Exception ex)
        {
            data["DbError"] = ex.Message;
        }

        // --- 2. Surface HTTP client circuit-breaker-like state ---
        var failures = _tracker.ConsecutiveFailures;
        var isDegraded = _tracker.IsDegraded;

        data["ConsecutiveFailures"] = failures;
        data["CircuitBreakerState"] = isDegraded ? "Open (degraded)" : "Closed";

        if (_tracker.LastFailure != DateTimeOffset.MinValue)
        {
            data["LastFailureUtc"] = _tracker.LastFailure.ToString("o");
            data["LastError"] = _tracker.LastError;
        }

        data["LastSuccessUtc"] = _tracker.LastSuccess.ToString("o");

        if (isDegraded)
        {
            return HealthCheckResult.Unhealthy(
                $"ExternalIntegrations HTTP client has {failures} consecutive failure(s) — circuit breaker likely open",
                data: data);
        }

        if (failures > 0)
        {
            return HealthCheckResult.Degraded(
                $"{failures} recent failure(s) on ExternalIntegrations client",
                data: data);
        }

        return HealthCheckResult.Healthy("All tenant integrations nominal", data);
    }
}
