using FastEndpoints;
using Winnow.Server.Entities;
using Winnow.Server.Infrastructure.Configuration;
using Winnow.Server.Infrastructure.MultiTenancy;
using Winnow.Server.Infrastructure.Persistence;
using System.Text.Json;

namespace Winnow.Server.Features.Integrations;

public class GetIntegrationConfigDetailEndpoint(WinnowDbContext db) : EndpointWithoutRequest<UpsertIntegrationConfigRequest>
{
    public override void Configure()
    {
        Get("/integrations/{Id}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var id = Route<Guid>("Id");
        var config = await db.IntegrationConfigs.FindAsync([id], ct);

        if (config == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var maskedJson = MaskSecrets(config.Provider, config.SettingsJson);

        var response = new UpsertIntegrationConfigRequest
        {
            Id = config.Id,
            Provider = config.Provider,
            SettingsJson = maskedJson,
            IsActive = config.IsActive
        };

        await Send.OkAsync(response, ct);
    }

    private string MaskSecrets(string provider, string json)
    {
        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            
            switch (provider.ToLowerInvariant())
            {
                case "github":
                    var gh = JsonSerializer.Deserialize<GitHubSettings>(json, options);
                    if (gh == null) return json;
                    if (!string.IsNullOrEmpty(gh.ApiKey)) gh.ApiKey = "******";
                    return JsonSerializer.Serialize(gh, options);
                
                case "trello":
                    var tr = JsonSerializer.Deserialize<TrelloSettings>(json, options);
                    if (tr == null) return json;
                    if (!string.IsNullOrEmpty(tr.ApiKey)) tr.ApiKey = "******";
                    if (!string.IsNullOrEmpty(tr.Token)) tr.Token = "******";
                    return JsonSerializer.Serialize(tr, options);

                case "jira":
                    var ji = JsonSerializer.Deserialize<JiraSettings>(json, options);
                    if (ji == null) return json;
                    if (!string.IsNullOrEmpty(ji.ApiToken)) ji.ApiToken = "******";
                    return JsonSerializer.Serialize(ji, options);
                
                default:
                    return json;
            }
        }
        catch
        {
            return json;
        }
    }
}
