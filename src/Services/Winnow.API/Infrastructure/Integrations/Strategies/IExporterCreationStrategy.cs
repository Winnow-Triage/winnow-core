using Winnow.Integrations;
using Winnow.Integrations.Domain;

namespace Winnow.API.Infrastructure.Integrations.Strategies;

/// <summary>
/// Strategy pattern interface for creating report exporters based on integration configuration types.
/// </summary>
public interface IExporterCreationStrategy
{
    /// <summary>
    /// Determines whether this strategy can handle the given integration configuration.
    /// </summary>
    /// <param name="config">The integration configuration to check.</param>
    /// <returns>True if this strategy can handle the configuration; otherwise, false.</returns>
    bool CanHandle(IntegrationConfig config);

    /// <summary>
    /// Creates an exporter instance for the given integration configuration.
    /// </summary>
    /// <param name="config">The integration configuration.</param>
    /// <param name="client">The HTTP client to use for the exporter.</param>
    /// <returns>An IReportExporter instance.</returns>
    IReportExporter Create(IntegrationConfig config, HttpClient client);
}