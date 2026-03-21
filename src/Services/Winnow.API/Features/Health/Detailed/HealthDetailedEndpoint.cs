using Winnow.API.Features.Health.Detail;
using System;
using System.Linq;
using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Winnow.API.Infrastructure.HealthChecks;

namespace Winnow.API.Features.Health.Detailed;

public sealed class HealthDetailedEndpoint(CachedHealthReportService cachedHealth, HealthCheckService fallbackCheck) : EndpointWithoutRequest<HealthDetailResponse>
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

        var response = HealthDetailResponse.FromHealthReport(report);

        // Always return 200 OK so the frontend can parse the JSON and display the failed checks
        await Send.ResponseAsync(response, StatusCodes.Status200OK, cancellation: ct);
    }
}