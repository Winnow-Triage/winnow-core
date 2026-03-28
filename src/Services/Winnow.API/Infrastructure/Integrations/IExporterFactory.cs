using Winnow.Integrations;

namespace Winnow.API.Infrastructure.Integrations;

/// <summary>
/// Factory interface for creating report exporters based on integration configurations.
/// </summary>
public interface IExporterFactory
{
    /// <summary>
    /// Gets an exporter instance, preferring the first active integration.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An IReportExporter instance.</returns>
    Task<IReportExporter> GetExporterAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets an exporter instance by specific integration configuration ID.
    /// </summary>
    /// <param name="configId">The integration configuration ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An IReportExporter instance.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the configuration is not found.</exception>
    Task<IReportExporter> GetExporterByIdAsync(Guid configId, CancellationToken ct = default);
}