using System.Text.Json;
using Winnow.Integrations.Domain;

namespace Winnow.Server.Infrastructure.Integrations.Strategies;

/// <summary>
/// Strategy for deserializing GitHub integration configuration.
/// </summary>
public class GitHubIntegrationConfigDeserializationStrategy : IIntegrationConfigDeserializationStrategy
{
    /// <inheritdoc />
    public bool CanHandle(string provider) => 
        provider.Equals("github", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public IntegrationConfig Deserialize(string settingsJson)
    {
        try
        {
            var config = JsonSerializer.Deserialize<GitHubConfig>(settingsJson);
            return config ?? new GitHubConfig();
        }
        catch (JsonException)
        {
            // If JSON is invalid, return a default configuration
            return new GitHubConfig();
        }
    }
}