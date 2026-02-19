using System.Text.Json;
using Winnow.Integrations.Domain;

namespace Winnow.Server.Infrastructure.Integrations.Strategies;

/// <summary>
/// Strategy for deserializing Trello integration configuration.
/// </summary>
internal class TrelloIntegrationConfigDeserializationStrategy : IIntegrationConfigDeserializationStrategy
{
    /// <inheritdoc />
    public bool CanHandle(string provider) => 
        provider.Equals("trello", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public IntegrationConfig Deserialize(string settingsJson)
    {
        try
        {
            var config = JsonSerializer.Deserialize<TrelloConfig>(settingsJson);
            return config ?? new TrelloConfig();
        }
        catch (JsonException)
        {
            // If JSON is invalid, return a default configuration
            return new TrelloConfig();
        }
    }
}