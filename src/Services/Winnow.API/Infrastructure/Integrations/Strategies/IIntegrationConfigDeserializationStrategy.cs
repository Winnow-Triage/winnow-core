using Winnow.Integrations.Domain;

namespace Winnow.API.Infrastructure.Integrations.Strategies;

/// <summary>
/// Strategy pattern interface for deserializing integration configuration based on provider string.
/// </summary>
public interface IIntegrationConfigDeserializationStrategy
{
    /// <summary>
    /// Determines whether this strategy can handle the given provider string.
    /// </summary>
    /// <param name="provider">The provider string to check (e.g., "github", "trello", "jira").</param>
    /// <returns>True if this strategy can handle the provider; otherwise, false.</returns>
    bool CanHandle(string provider);

    /// <summary>
    /// Deserializes the settings JSON into the appropriate IntegrationConfig type.
    /// </summary>
    /// <param name="settingsJson">The JSON string containing configuration settings.</param>
    /// <returns>An IntegrationConfig instance of the appropriate concrete type.</returns>
    IntegrationConfig Deserialize(string settingsJson);
}