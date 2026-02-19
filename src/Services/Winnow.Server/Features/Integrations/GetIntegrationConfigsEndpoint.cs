using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Integrations;

public record IntegrationDto(Guid Id, string Provider, string Name);

public sealed class GetIntegrationConfigsEndpoint(WinnowDbContext db) : EndpointWithoutRequest<List<IntegrationDto>>
{
    public override void Configure()
    {
        Get("/integrations");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        try
        {
            var integrations = await db.Integrations
                .AsNoTracking()
                .Where(i => i.IsActive)
                .Select(i => new { i.Id, i.Provider })
                .ToListAsync(ct);

            // In a real app, we might parse Config to get a user-friendly name (e.g. Board Name)
            // For now, return Provider name.
            var dtos = integrations.Select(i => new IntegrationDto(i.Id, i.Provider, $"{i.Provider} Integration")).ToList();
            
            await Send.OkAsync(dtos, cancellation: ct);
        }
        catch
        {
            // If table doesn't exist
            await Send.OkAsync([], cancellation: ct);
        }
    }
}
