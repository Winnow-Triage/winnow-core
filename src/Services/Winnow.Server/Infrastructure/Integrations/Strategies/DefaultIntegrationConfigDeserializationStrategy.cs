using Winnow.Integrations.Domain;

namespace Winnow.Server.Infrastructure.Integrations.Strategies;

/// <summary>
/// Default strategy that throws an exception for unsupported providers.
/// This matches the behavior of the original switch statement's default case.
/// </summary>
internal class DefaultIntegrationConfigDeserializationStrategy : IIntegrationConfigDeserializationStrategy
{
    /// <inheritdoc />
    public bool CanHandle(string provider) => false; // Never handles any provider - this is a fallback

    /// <inheritdoc />
    public IntegrationConfig Deserialize(string settingsJson)
    {
        // This method should never be called since CanHandle returns false
        throw new InvalidOperationException("Default strategy should not be used for deserialization");
    }
}