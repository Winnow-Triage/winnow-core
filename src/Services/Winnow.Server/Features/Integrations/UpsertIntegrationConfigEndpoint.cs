using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Entities;
using Winnow.Server.Infrastructure.MultiTenancy;
using Winnow.Server.Infrastructure.Persistence;
using Winnow.Server.Infrastructure.Configuration;
using System.Text.Json;

namespace Winnow.Server.Features.Integrations;

public class UpsertIntegrationConfigRequest
{
    public Guid? Id { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string SettingsJson { get; set; } = "{}";
    public bool IsActive { get; set; } = true;
}

public class UpsertIntegrationConfigEndpoint(WinnowDbContext db) : Endpoint<UpsertIntegrationConfigRequest, IntegrationConfig>
{
    public override void Configure()
    {
        Post("/integrations");
        AllowAnonymous();
    }

    public override async Task HandleAsync(UpsertIntegrationConfigRequest req, CancellationToken ct)
    {
        IntegrationConfig? config = null;

        if (req.Id.HasValue)
        {
            config = await db.IntegrationConfigs.FindAsync([req.Id.Value], ct);
            if (config == null)
            {
                await Send.NotFoundAsync(ct);
                return;
            }
        }
        else
        {
            config = new IntegrationConfig();
            await db.IntegrationConfigs.AddAsync(config, ct);
        }

        config.Provider = req.Provider;
        config.IsActive = req.IsActive;

        // Smart merge: if existing config exists, check for masked values in request and preserve originals
        if (req.Id.HasValue && config.Id == req.Id.Value) 
        {
             config.SettingsJson = MergeSettings(req.Provider, config.SettingsJson, req.SettingsJson);
        }
        else
        {
            config.SettingsJson = req.SettingsJson;
        }

        await db.SaveChangesAsync(ct);

        await Send.OkAsync(config, ct);
    }

    private string MergeSettings(string provider, string originalJson, string newJson)
    {
        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            
            switch (provider.ToLowerInvariant())
            {
                case "github":
                    var oldGh = JsonSerializer.Deserialize<GitHubSettings>(originalJson, options);
                    var newGh = JsonSerializer.Deserialize<GitHubSettings>(newJson, options);
                    if (oldGh == null || newGh == null) return newJson;

                    if (newGh.ApiKey == "******") newGh.ApiKey = oldGh.ApiKey;
                    return JsonSerializer.Serialize(newGh, options);
                
                case "trello":
                    var oldTr = JsonSerializer.Deserialize<TrelloSettings>(originalJson, options);
                    var newTr = JsonSerializer.Deserialize<TrelloSettings>(newJson, options);
                    if (oldTr == null || newTr == null) return newJson;

                    if (newTr.ApiKey == "******") newTr.ApiKey = oldTr.ApiKey;
                    if (newTr.Token == "******") newTr.Token = oldTr.Token;
                    return JsonSerializer.Serialize(newTr, options);

                case "jira":
                    var oldJi = JsonSerializer.Deserialize<JiraSettings>(originalJson, options);
                    var newJi = JsonSerializer.Deserialize<JiraSettings>(newJson, options);
                    if (oldJi == null || newJi == null) return newJson;

                    if (newJi.ApiToken == "******") newJi.ApiToken = oldJi.ApiToken;
                    return JsonSerializer.Serialize(newJi, options);
                
                default:
                    return newJson;
            }
        }
        catch
        {
            return newJson;
        }
    }
}
