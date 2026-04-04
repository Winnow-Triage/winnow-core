using System.Text.Json;
using Winnow.Integrations.Domain;

namespace Winnow.API.Infrastructure.Integrations.Strategies;

/// <summary>
/// Strategy for deserializing Discord integration configuration.
/// </summary>
internal class DiscordIntegrationConfigDeserializationStrategy : IIntegrationConfigDeserializationStrategy
{
    /// <inheritdoc />
    public bool CanHandle(string provider) =>
        provider.Equals("discord", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public IntegrationConfig Deserialize(string settingsJson)
    {
        try
        {
            var config = JsonSerializer.Deserialize<DiscordConfig>(settingsJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            return config ?? new DiscordConfig();
        }
        catch (JsonException)
        {
            // If JSON is invalid, return a default configuration
            return new DiscordConfig();
        }
    }
}
