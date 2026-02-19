using Winnow.Integrations;
using Winnow.Integrations.Domain;

namespace Winnow.Server.Infrastructure.Integrations.Strategies;

/// <summary>
/// Strategy for creating Trello exporters.
/// </summary>
internal class TrelloExporterCreationStrategy : IExporterCreationStrategy
{
    /// <inheritdoc />
    public bool CanHandle(IntegrationConfig config) => config is TrelloConfig;

    /// <inheritdoc />
    public IReportExporter Create(IntegrationConfig config, HttpClient client)
    {
        if (config is not TrelloConfig trelloConfig)
            throw new ArgumentException($"Expected {nameof(TrelloConfig)} but got {config?.GetType().Name}", nameof(config));

        return new TrelloExporter(client, trelloConfig.ApiKey, trelloConfig.Token, trelloConfig.ListId);
    }
}