using FastEndpoints;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Winnow.Server.Infrastructure.HealthChecks;

namespace Winnow.Server.Features.Health;

public class HealthDetailedEndpoint : EndpointWithoutRequest
{
    private readonly HealthCheckService _healthCheckService;

    public HealthDetailedEndpoint(HealthCheckService healthCheckService)
    {
        _healthCheckService = healthCheckService;
    }

    public override void Configure()
    {
        Get("/health/detailed");
        Options(x => x
            .WithTags("Health")
            .RequireAuthorization(r => r.RequireRole("SuperAdmin")));
        Description(x => x
            .WithName("HealthDetailed")
            .Produces<object>(200, "application/json")
            .Produces<object>(503, "application/json")
            .WithTags("Health"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        // Detailed health check: runs all checks
        var report = await _healthCheckService.CheckHealthAsync(ct);

        // Use the JSON writer utility
        await HealthCheckJsonWriter.WriteHealthCheckResponse(HttpContext, report);
        
        // Set appropriate status code
        HttpContext.Response.StatusCode = report.Status == HealthStatus.Healthy 
            ? StatusCodes.Status200OK 
            : StatusCodes.Status503ServiceUnavailable;
    }
}