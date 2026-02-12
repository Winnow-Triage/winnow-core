using System.Text.Json;
using Winnow.Integrations;
using Winnow.Server.Infrastructure.Configuration;
using Winnow.Server.Infrastructure.MultiTenancy;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Infrastructure.Integrations;

public class ExporterFactory(
    IHttpClientFactory httpClientFactory,
    WinnowDbContext dbContext,
    ITenantContext tenantContext)
{
    public ITicketExporter GetExporter()
    {
        // 1. Resolve Tenant context
        var tenantId = tenantContext.TenantId ?? "default";

        // 2. Query the database for an active integration config
        var config = dbContext.IntegrationConfigs
            .Where(c => c.IsActive)
            .FirstOrDefault();

        if (config == null) return new NullExporter();

        var client = httpClientFactory.CreateClient("Exporter");
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        return config.Provider.ToLowerInvariant() switch
        {
            "github" => CreateGitHubExporter(config.SettingsJson, client, options),
            "trello" => CreateTrelloExporter(config.SettingsJson, client, options),
            "jira" => CreateJiraExporter(config.SettingsJson, client, options),
            _ => new NullExporter()
        };
    }

    private static ITicketExporter CreateGitHubExporter(string json, HttpClient client, JsonSerializerOptions options)
    {
        var s = JsonSerializer.Deserialize<GitHubSettings>(json, options);
        return s == null ? new NullExporter() : new GitHubExporter(client, s.ApiKey, s.Owner, s.Repo);
    }

    private static ITicketExporter CreateTrelloExporter(string json, HttpClient client, JsonSerializerOptions options)
    {
        var s = JsonSerializer.Deserialize<TrelloSettings>(json, options);
        return s == null ? new NullExporter() : new TrelloExporter(client, s.ApiKey, s.Token, s.ListId);
    }

    private static ITicketExporter CreateJiraExporter(string json, HttpClient client, JsonSerializerOptions options)
    {
        var s = JsonSerializer.Deserialize<JiraSettings>(json, options);
        return s == null ? new NullExporter() : new JiraExporter(client, s.BaseUrl, s.UserEmail, s.ApiToken, s.ProjectKey);
    }
}

public class NullExporter : ITicketExporter
{
    public Task ExportTicketAsync(string title, string description, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
