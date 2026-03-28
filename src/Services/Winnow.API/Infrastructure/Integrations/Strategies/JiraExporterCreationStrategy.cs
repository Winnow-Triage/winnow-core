using Winnow.Integrations;
using Winnow.Integrations.Domain;

namespace Winnow.API.Infrastructure.Integrations.Strategies;

/// <summary>
/// Strategy for creating Jira exporters.
/// </summary>
internal class JiraExporterCreationStrategy : IExporterCreationStrategy
{
    /// <inheritdoc />
    public bool CanHandle(IntegrationConfig config) => config is JiraConfig;

    /// <inheritdoc />
    public IReportExporter Create(IntegrationConfig config, HttpClient client)
    {
        if (config is not JiraConfig jiraConfig)
            throw new ArgumentException($"Expected {nameof(JiraConfig)} but got {config?.GetType().Name}", nameof(config));

        return new JiraExporter(client, jiraConfig.BaseUrl, jiraConfig.UserEmail, jiraConfig.ApiToken, jiraConfig.ProjectKey);
    }
}