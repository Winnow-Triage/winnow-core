using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Entities;
using Winnow.Server.Infrastructure.MultiTenancy;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Integrations;

public record IntegrationConfigDto(Guid Id, string Provider, string Name);

public class GetIntegrationConfigsEndpoint(WinnowDbContext db) : EndpointWithoutRequest<List<IntegrationConfigDto>>
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
            var configs = await db.IntegrationConfigs
                .AsNoTracking()
                .Where(c => c.IsActive)
                .Select(c => new { c.Id, c.Provider })
                .ToListAsync(ct);

            // In a real app, we might parse SettingsJson to get a user-friendly name (e.g. Board Name)
            // For now, return Provider name.
            var dtos = configs.Select(c => new IntegrationConfigDto(c.Id, c.Provider, $"{c.Provider} Integration")).ToList();
            
            await Send.OkAsync(dtos, cancellation: ct);
        }
        catch
        {
            // If table doesn't exist
            await Send.OkAsync([], cancellation: ct);
        }
    }
}
