using FastEndpoints;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Winnow.Server.Features.Health;

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

        if (report.Status == HealthStatus.Healthy)
        {
            await Send.OkAsync("Healthy", cancellation: ct);
        }
        else
        {
            await Send.ResponseAsync("Unhealthy", StatusCodes.Status503ServiceUnavailable, cancellation: ct);
        }
    }
}