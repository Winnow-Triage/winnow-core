using System;
using System.Linq;
using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Winnow.Server.Features.Health;

public sealed class HealthDetailedEndpoint(HealthCheckService healthCheckService) : EndpointWithoutRequest<object>
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
        // Detailed health check: runs all checks
        var report = await healthCheckService.CheckHealthAsync(ct);

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
                // Circuit breaker status could be added here by querying resilience pipeline metrics
                externalIntegrations = "Configured",
                httpClients = "With resilience pipeline"
            }
        };

        // Always return 200 OK so the frontend can parse the JSON and display the failed checks
        // instead of Axios throwing a Network Error exception.
        await Send.ResponseAsync(response, StatusCodes.Status200OK, cancellation: ct);
    }
}