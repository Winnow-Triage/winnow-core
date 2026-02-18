using Winnow.Integrations;
using Winnow.Integrations.Domain;

namespace Winnow.Server.Infrastructure.Integrations.Strategies;

/// <summary>
/// Default strategy that returns a NullExporter for any configuration type.
/// This serves as a fallback strategy when no specific strategy matches.
/// </summary>
public class DefaultExporterCreationStrategy : IExporterCreationStrategy
{
    /// <inheritdoc />
    public bool CanHandle(IntegrationConfig config) => true; // Always handles any configuration

    /// <inheritdoc />
    public IReportExporter Create(IntegrationConfig config, HttpClient client)
    {
        // Returns a NullExporter for any configuration
        // This matches the behavior of the original switch statement's default case
        return new NullExporter();
    }
}