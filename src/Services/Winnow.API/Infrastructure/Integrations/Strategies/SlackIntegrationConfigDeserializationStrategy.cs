using System.Text.Json;
using Winnow.Integrations.Domain;

namespace Winnow.API.Infrastructure.Integrations.Strategies;

/// <summary>
/// Strategy for deserializing Slack integration configuration.
/// </summary>
internal class SlackIntegrationConfigDeserializationStrategy : IIntegrationConfigDeserializationStrategy
{
    /// <inheritdoc />
    public bool CanHandle(string provider) =>
        provider.Equals("slack", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public IntegrationConfig Deserialize(string settingsJson)
    {
        try
        {
            var config = JsonSerializer.Deserialize<SlackConfig>(settingsJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            return config ?? new SlackConfig();
        }
        catch (JsonException)
        {
            // If JSON is invalid, return a default configuration
            return new SlackConfig();
        }
    }
}
