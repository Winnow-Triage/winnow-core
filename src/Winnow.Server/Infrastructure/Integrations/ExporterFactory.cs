using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Winnow.Integrations;
using Winnow.Server.Entities;
using Winnow.Server.Infrastructure.Configuration;
using Winnow.Server.Infrastructure.MultiTenancy;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Infrastructure.Integrations;

public class ExporterFactory(
    IHttpClientFactory httpClientFactory,
    WinnowDbContext dbContext,
    ITenantContext tenantContext)
{
    public async Task<ITicketExporter> GetExporterAsync(CancellationToken ct = default)
    {
        // Default behavior: Pick the first active one (or null)
        try
        {
            var config = await dbContext.IntegrationConfigs
                .AsNoTracking()
                .Where(c => c.IsActive)
                .FirstOrDefaultAsync(ct);

            if (config == null) return new NullExporter();
            return CreateExporterFromConfig(config);
        }
        catch { return new NullExporter(); }
    }

    public async Task<ITicketExporter> GetExporterByIdAsync(Guid configId, CancellationToken ct = default)
    {
        try
        {
            var config = await dbContext.IntegrationConfigs
                .AsNoTracking()
                .Where(c => c.Id == configId && c.IsActive)
                .FirstOrDefaultAsync(ct);

            if (config == null) return new NullExporter();
            return CreateExporterFromConfig(config);
        }
        catch { return new NullExporter(); }
    }

    private ITicketExporter CreateExporterFromConfig(IntegrationConfig config)
    {
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

    private ITicketExporter CreateGitHubExporter(string json, HttpClient client, JsonSerializerOptions options)
    {
        try
        {
            var s = JsonSerializer.Deserialize<GitHubSettings>(json, options);
            return s == null ? new NullExporter() : new GitHubExporter(client, s.ApiKey, s.Owner, s.Repo);
        }
        catch { return new NullExporter(); }
    }

    private ITicketExporter CreateTrelloExporter(string json, HttpClient client, JsonSerializerOptions options)
    {
        try
        {
            var s = JsonSerializer.Deserialize<TrelloSettings>(json, options);
            return s == null ? new NullExporter() : new TrelloExporter(client, s.ApiKey, s.Token, s.ListId);
        }
        catch { return new NullExporter(); }
    }

    private ITicketExporter CreateJiraExporter(string json, HttpClient client, JsonSerializerOptions options)
    {
        try
        {
            var s = JsonSerializer.Deserialize<JiraSettings>(json, options);
            return s == null ? new NullExporter() : new JiraExporter(client, s.BaseUrl, s.UserEmail, s.ApiToken, s.ProjectKey);
        }
        catch { return new NullExporter(); }
    }
}

public class NullExporter : ITicketExporter
{
    public Task ExportTicketAsync(string title, string description, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
