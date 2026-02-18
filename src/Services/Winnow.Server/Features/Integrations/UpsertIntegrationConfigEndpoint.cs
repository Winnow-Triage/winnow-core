using FastEndpoints;
using Winnow.Server.Entities;
using Winnow.Integrations.Domain;
using Winnow.Server.Infrastructure.Persistence;
using System.Text.Json;

namespace Winnow.Server.Features.Integrations;

public class UpsertIntegrationConfigRequest
{
    public Guid? Id { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string SettingsJson { get; set; } = "{}";
    public bool IsActive { get; set; } = true;
}

public class UpsertIntegrationConfigEndpoint(WinnowDbContext db) : Endpoint<UpsertIntegrationConfigRequest, Integration>
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

        // Deserialize request into concrete domain record based on provider
        IntegrationConfig newConfig = req.Provider.ToLowerInvariant() switch
        {
            "github" => JsonSerializer.Deserialize<GitHubConfig>(req.SettingsJson) ?? new GitHubConfig(),
            "trello" => JsonSerializer.Deserialize<TrelloConfig>(req.SettingsJson) ?? new TrelloConfig(),
            "jira" => JsonSerializer.Deserialize<JiraConfig>(req.SettingsJson) ?? new JiraConfig(),
            _ => throw new ArgumentException($"Unsupported provider: {req.Provider}")
        };
        
        // Use the polymorphic domain model to update configuration
        integration.UpdateConfig(newConfig);

        await db.SaveChangesAsync(ct);

        await Send.OkAsync(integration, ct);
    }
}
