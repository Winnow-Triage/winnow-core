using FastEndpoints;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Winnow.Server.Features.Health;

public class HealthEndpoint : EndpointWithoutRequest
{
    private readonly HealthCheckService _healthCheckService;

    public HealthEndpoint(HealthCheckService healthCheckService)
    {
        _healthCheckService = healthCheckService;
    }

    public override void Configure()
    {
        Get("/health");
        AllowAnonymous();
        Options(x => x.WithTags("Health"));
        Description(x => x
            .WithName("Health")
            .Produces<string>(200, "text/plain")
            .Produces<string>(503, "text/plain")
            .WithTags("Health"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        // Simple aggregate health endpoint - uses readiness checks
        var report = await _healthCheckService.CheckHealthAsync(
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