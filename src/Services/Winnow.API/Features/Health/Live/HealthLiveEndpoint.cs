using FastEndpoints;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Winnow.API.Features.Health.Live;

public sealed class HealthLiveEndpoint : EndpointWithoutRequest
{
    public override void Configure()
    {
        Get("/health/live");
        AllowAnonymous();
        Options(x => x.WithTags("Health"));
        Description(x => x
            .WithName("HealthLive")
            .Produces<string>(200, "text/plain")
            .Produces<string>(503, "text/plain")
            .WithTags("Health"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        // Liveness check: only checks if the HTTP pipeline is responsive
        // Predicate = _ => false means no health checks are executed
        await Send.OkAsync("Healthy", cancellation: ct);
    }
}