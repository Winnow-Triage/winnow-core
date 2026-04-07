using System.Text.Json;
using Winnow.Integrations.Domain;

namespace Winnow.API.Infrastructure.Integrations.Strategies;

/// <summary>
/// Strategy for deserializing Email integration configuration.
/// </summary>
internal class EmailIntegrationConfigDeserializationStrategy : IIntegrationConfigDeserializationStrategy
{
    /// <inheritdoc />
    public bool CanHandle(string provider) =>
        provider.Equals("email", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public IntegrationConfig Deserialize(string settingsJson)
    {
        try
        {
            var config = JsonSerializer.Deserialize<EmailConfig>(settingsJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            return config ?? new EmailConfig();
        }
        catch (JsonException)
        {
            // If JSON is invalid, return a default configuration
            return new EmailConfig();
        }
    }
}
