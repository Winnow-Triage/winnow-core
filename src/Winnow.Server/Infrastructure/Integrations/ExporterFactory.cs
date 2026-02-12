using Winnow.Integrations;
using Winnow.Server.Infrastructure.Configuration;
using Winnow.Server.Infrastructure.MultiTenancy;

namespace Winnow.Server.Infrastructure.Integrations;

public class ExporterFactory(
    IHttpClientFactory httpClientFactory,
    IntegrationSettings settings,
    ITenantContext tenantContext)
{
    public ITicketExporter GetExporter()
    {
        // For now, we use a global setting, but we could easily switch to 
        // per-tenant logic here by looking at tenantContext.TenantId.
        var tenantId = tenantContext.TenantId ?? "default";

        var client = httpClientFactory.CreateClient("Exporter");

        // Simple heuristic: If GitHub owner is set, use GitHub. 
        // In a real app, this would be a 'PreferredProvider' setting.
        if (!string.IsNullOrEmpty(settings.GitHub.Owner))
        {
            return new GitHubExporter(client, settings.GitHub.ApiKey, settings.GitHub.Owner, settings.GitHub.Repo);
        }

        if (!string.IsNullOrEmpty(settings.Trello.ListId))
        {
            return new TrelloExporter(client, settings.Trello.ApiKey, settings.Trello.Token, settings.Trello.ListId);
        }

        if (!string.IsNullOrEmpty(settings.Jira.BaseUrl))
        {
            return new JiraExporter(client, settings.Jira.BaseUrl, settings.Jira.UserEmail, settings.Jira.ApiToken, settings.Jira.ProjectKey);
        }

        // Fallback or Null Object
        return new NullExporter();
    }
}

public class NullExporter : ITicketExporter
{
    public Task ExportTicketAsync(string title, string description, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
