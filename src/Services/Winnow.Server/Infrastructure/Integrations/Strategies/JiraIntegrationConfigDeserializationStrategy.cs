using System.Text.Json;
using Winnow.Integrations.Domain;

namespace Winnow.Server.Infrastructure.Integrations.Strategies;

/// <summary>
/// Strategy for deserializing Jira integration configuration.
/// </summary>
internal class JiraIntegrationConfigDeserializationStrategy : IIntegrationConfigDeserializationStrategy
{
    /// <inheritdoc />
    public bool CanHandle(string provider) =>
        provider.Equals("jira", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public IntegrationConfig Deserialize(string settingsJson)
    {
        try
        {
            var config = JsonSerializer.Deserialize<JiraConfig>(settingsJson);
            return config ?? new JiraConfig();
        }
        catch (JsonException)
        {
            // If JSON is invalid, return a default configuration
            return new JiraConfig();
        }
    }
}