using FastEndpoints;
using Winnow.Server.Entities;
using Winnow.Integrations.Domain;
using Winnow.Server.Infrastructure.Persistence;
using Winnow.Server.Infrastructure.Integrations.Strategies;
using System.Text.Json;

namespace Winnow.Server.Features.Integrations;

public class UpsertIntegrationConfigRequest
{
    public Guid? Id { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string SettingsJson { get; set; } = "{}";
    public bool IsActive { get; set; } = true;
}

public sealed class UpsertIntegrationConfigEndpoint(
    WinnowDbContext db,
    IEnumerable<IIntegrationConfigDeserializationStrategy> deserializationStrategies) 
    : Endpoint<UpsertIntegrationConfigRequest, Integration>
{
    public override void Configure()
    {
        Post("/integrations");
        AllowAnonymous();
    }

    public override async Task HandleAsync(UpsertIntegrationConfigRequest req, CancellationToken ct)
    {
        Integration? integration;

        if (req.Id.HasValue)
        {
            integration = await db.Integrations.FindAsync([req.Id.Value], ct);
            if (integration == null)
            {
                await Send.NotFoundAsync(ct);
                return;
            }
        }
        else
        {
            integration = new Integration();
            await db.Integrations.AddAsync(integration, ct);
        }

        integration.Provider = req.Provider;
        integration.IsActive = req.IsActive;

        // Find the appropriate deserialization strategy for this provider
        var strategy = deserializationStrategies.FirstOrDefault(s => s.CanHandle(req.Provider))
            ?? throw new ArgumentException($"Unsupported provider: {req.Provider}");

        // Use the strategy to deserialize the configuration
        IntegrationConfig newConfig = strategy.Deserialize(req.SettingsJson);
        
        // Use the polymorphic domain model to update configuration
        integration.UpdateConfig(newConfig);

        await db.SaveChangesAsync(ct);

        await Send.OkAsync(integration, ct);
    }
}
