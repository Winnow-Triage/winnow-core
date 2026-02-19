using Microsoft.EntityFrameworkCore;
using Winnow.Integrations;
using Winnow.Integrations.Domain;
using Winnow.Server.Entities;
using Winnow.Server.Infrastructure.Integrations.Strategies;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Infrastructure.Integrations;

public class ExporterFactory(
    IHttpClientFactory httpClientFactory,
    WinnowDbContext dbContext,
    IEnumerable<IExporterCreationStrategy> strategies) : IExporterFactory
{
    public async Task<IReportExporter> GetExporterAsync(CancellationToken ct = default)
    {
        // Default behavior: Pick the first active one (or null)
        var integration = await dbContext.Integrations
            .AsNoTracking()
            .Where(i => i.IsActive)
            .FirstOrDefaultAsync(ct);

        if (integration == null) return new NullExporter();
        return CreateExporterFromIntegration(integration);
    }

    public async Task<IReportExporter> GetExporterByIdAsync(Guid configId, CancellationToken ct = default)
    {
        var integration = await dbContext.Integrations
            .AsNoTracking()
            .Where(i => i.Id == configId && i.IsActive)
            .FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException($"Integration config {configId} not found");

        return CreateExporterFromIntegration(integration);
    }

    private IReportExporter CreateExporterFromIntegration(Integration integration)
    {
        var client = httpClientFactory.CreateClient("Exporter");
        var config = integration.Config;

        // Find the appropriate strategy for this configuration type
        var strategy = strategies.FirstOrDefault(s => s.CanHandle(config))
            ?? throw new InvalidOperationException($"No exporter creation strategy found for configuration type: {config?.GetType().Name}");

        return strategy.Create(config, client);
    }
}

public class NullExporter : IReportExporter
{
    public Task<string> ExportReportAsync(string title, string description, CancellationToken cancellationToken)
    {
        return Task.FromResult(string.Empty);
    }
}
