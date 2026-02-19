using System.Text.Json;
using FastEndpoints;
using Winnow.Integrations.Domain;
using Winnow.Server.Entities;
using Winnow.Server.Infrastructure.Integrations.Strategies;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Integrations;

/// <summary>
/// Request to create or update an integration configuration.
/// </summary>
public class UpsertIntegrationConfigRequest
{
    /// <summary>
    /// ID of the configuration (null for create).
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// Provider name (e.g., Jira, GitHub).
    /// </summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// JSON string containing provider-specific settings.
    /// </summary>
    public string SettingsJson { get; set; } = "{}";

    /// <summary>
    /// Whether the integration is active.
    /// </summary>
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
        Summary(s =>
        {
            s.Summary = "Create/Update integration";
            s.Description = "Creates or updates an integration configuration. Provider settings must be valid JSON.";
            s.Response<Integration>(200, "Integration saved successfully");
            s.Response(404, "Integration to update not found");
        });
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
