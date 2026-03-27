using FastEndpoints;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Winnow.API.Features.Health.Ready;

public sealed class HealthReadyEndpoint(HealthCheckService healthCheckService) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Get("/health/ready");
        AllowAnonymous();
        Options(x => x.WithTags("Health"));
        Description(x => x
            .WithName("HealthReady")
            .Produces<string>(200, "text/plain")
            .Produces<string>(503, "text/plain")
            .WithTags("Health"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        // Readiness check: only checks with "ready" tag
        var report = await healthCheckService.CheckHealthAsync(
            check => check.Tags.Contains("ready"),
            ct);

        if (report.Status == HealthStatus.Unhealthy)
        {
            await Send.ResponseAsync("Unhealthy", StatusCodes.Status503ServiceUnavailable, cancellation: ct);
        }
        else
        {
            // Both Healthy and Degraded are acceptable for readiness
            await Send.OkAsync(report.Status == HealthStatus.Healthy ? "Healthy" : "Degraded", cancellation: ct);
        }
    }
}