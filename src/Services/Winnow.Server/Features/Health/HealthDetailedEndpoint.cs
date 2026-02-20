using System;
using System.Linq;
using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Winnow.Server.Infrastructure.HealthChecks;

namespace Winnow.Server.Features.Health;

public sealed class HealthDetailedEndpoint(CachedHealthReportService cachedHealth, HealthCheckService fallbackCheck) : EndpointWithoutRequest<object>
{
    public override void Configure()
    {
        Get("/health/detailed");
        Options(x => x
            .WithTags("Health")
            .RequireAuthorization(r => r.RequireRole("SuperAdmin")));
        Description(x => x
            .WithName("HealthDetailed")
            .Produces<object>(200, "application/json")
            .WithTags("Health"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        // Try getting the background-polled report first
        var report = cachedHealth.Report;

        // If it's null (the publisher hasn't fired its first tick yet), fallback briefly
        if (report == null)
        {
            report = await fallbackCheck.CheckHealthAsync(ct);
        }

        var response = new
        {
            status = report.Status.ToString(),
            totalDuration = report.TotalDuration.ToString(),
            utcTimestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                duration = entry.Value.Duration.ToString(),
                description = entry.Value.Description,
                data = entry.Value.Data,
                exception = entry.Value.Exception?.Message,
                tags = entry.Value.Tags
            }).ToList(),
            circuitBreakers = new
            {
                externalIntegrations = "Configured",
                httpClients = "With resilience pipeline"
            }
        };

        // Always return 200 OK so the frontend can parse the JSON and display the failed checks
        await Send.ResponseAsync(response, StatusCodes.Status200OK, cancellation: ct);
    }
}