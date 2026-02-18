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
    WinnowDbContext dbContext)
{
    public async Task<IReportExporter> GetExporterAsync(CancellationToken ct = default)
    {
        // Default behavior: Pick the first active one (or null)
        var config = await dbContext.IntegrationConfigs
            .AsNoTracking()
            .Where(c => c.IsActive)
            .FirstOrDefaultAsync(ct);

        if (config == null) return new NullExporter();
        return CreateExporterFromConfig(config);
    }

    public async Task<IReportExporter> GetExporterByIdAsync(Guid configId, CancellationToken ct = default)
    {
        var config = await dbContext.IntegrationConfigs
            .AsNoTracking()
            .Where(c => c.Id == configId && c.IsActive)
            .FirstOrDefaultAsync(ct);

        if (config == null) throw new KeyNotFoundException($"Integration config {configId} not found");
        return CreateExporterFromConfig(config);
    }

    private IReportExporter CreateExporterFromConfig(IntegrationConfig config)
    {
        var client = httpClientFactory.CreateClient("Exporter");
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        return config.Provider.ToLowerInvariant() switch
        {
            "github" => CreateGitHubExporter(config.SettingsJson, client, options),
            "trello" => CreateTrelloExporter(config.SettingsJson, client, options),
            "jira" => CreateJiraExporter(config.SettingsJson, client, options),
            _ => new NullExporter() // Or throw?
        };
    }

    private IReportExporter CreateGitHubExporter(string json, HttpClient client, JsonSerializerOptions options)
    {
        var s = JsonSerializer.Deserialize<GitHubSettings>(json, options);
        if (s == null) throw new InvalidOperationException("Failed to deserialize GitHub settings");
        return new GitHubExporter(client, s.ApiKey, s.Owner, s.Repo);
    }

    private IReportExporter CreateTrelloExporter(string json, HttpClient client, JsonSerializerOptions options)
    {
        var s = JsonSerializer.Deserialize<TrelloSettings>(json, options);
        if (s == null) throw new InvalidOperationException("Failed to deserialize Trello settings");
        return new TrelloExporter(client, s.ApiKey, s.Token, s.ListId);
    }

    private IReportExporter CreateJiraExporter(string json, HttpClient client, JsonSerializerOptions options)
    {
        var s = JsonSerializer.Deserialize<JiraSettings>(json, options);
        if (s == null) throw new InvalidOperationException("Failed to deserialize Jira settings");
        return new JiraExporter(client, s.BaseUrl, s.UserEmail, s.ApiToken, s.ProjectKey);
    }
}

public class NullExporter : IReportExporter
{
    public Task<string> ExportReportAsync(string title, string description, CancellationToken cancellationToken)
    {
        return Task.FromResult(string.Empty);
    }
}
