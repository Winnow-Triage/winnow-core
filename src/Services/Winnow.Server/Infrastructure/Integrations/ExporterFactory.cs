using Microsoft.EntityFrameworkCore;
using Winnow.Integrations;
using Winnow.Integrations.Domain;
using Winnow.Server.Entities;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Infrastructure.Integrations;

public class ExporterFactory(
    IHttpClientFactory httpClientFactory,
    WinnowDbContext dbContext)
{
    public async Task<IReportExporter> GetExporterAsync(CancellationToken ct = default)
    {
        // Default behavior: Pick the first active one (or null)
        var integration = await dbContext.Integrations
            .AsNoTracking()
            .Where(i => i.IsActive)
            .FirstOrDefaultAsync(ct);

        if (integration == null) return new NullExporter();
        return CreateExporterFromIntegration(integration);
    }

    public async Task<IReportExporter> GetExporterByIdAsync(Guid configId, CancellationToken ct = default)
    {
        var integration = await dbContext.Integrations
            .AsNoTracking()
            .Where(i => i.Id == configId && i.IsActive)
            .FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException($"Integration config {configId} not found");

        return CreateExporterFromIntegration(integration);
    }

    private IReportExporter CreateExporterFromIntegration(Integration integration)
    {
        var client = httpClientFactory.CreateClient("Exporter");

        return integration.Config switch
        {
            GitHubConfig github => CreateGitHubExporter(github, client),
            TrelloConfig trello => CreateTrelloExporter(trello, client),
            JiraConfig jira => CreateJiraExporter(jira, client),
            _ => new NullExporter() // Or throw?
        };
    }

    private static GitHubExporter CreateGitHubExporter(GitHubConfig config, HttpClient client)
    {
        return new GitHubExporter(client, config.ApiKey, config.Owner, config.Repo);
    }

    private static TrelloExporter CreateTrelloExporter(TrelloConfig config, HttpClient client)
    {
        return new TrelloExporter(client, config.ApiKey, config.Token, config.ListId);
    }

    private static JiraExporter CreateJiraExporter(JiraConfig config, HttpClient client)
    {
        return new JiraExporter(client, config.BaseUrl, config.UserEmail, config.ApiToken, config.ProjectKey);
    }
}

public class NullExporter : IReportExporter
{
    public Task<string> ExportReportAsync(string title, string description, CancellationToken cancellationToken)
    {
        return Task.FromResult(string.Empty);
    }
}
