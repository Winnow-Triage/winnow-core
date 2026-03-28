using Winnow.Integrations;
using Winnow.Integrations.Domain;

namespace Winnow.API.Infrastructure.Integrations.Strategies;

/// <summary>
/// Strategy for creating GitHub exporters.
/// </summary>
internal class GitHubExporterCreationStrategy : IExporterCreationStrategy
{
    /// <inheritdoc />
    public bool CanHandle(IntegrationConfig config) => config is GitHubConfig;

    /// <inheritdoc />
    public IReportExporter Create(IntegrationConfig config, HttpClient client)
    {
        if (config is not GitHubConfig githubConfig)
            throw new ArgumentException($"Expected {nameof(GitHubConfig)} but got {config?.GetType().Name}", nameof(config));

        return new GitHubExporter(client, githubConfig.ApiKey, githubConfig.Owner, githubConfig.Repo);
    }
}