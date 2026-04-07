using Microsoft.EntityFrameworkCore;
using Winnow.API.Infrastructure.Integrations;
using Winnow.API.Infrastructure.Persistence;
using Winnow.Contracts;

namespace Winnow.API.Features.Clusters.Events;

/// <summary>
/// Wolverine message handler that handles the final delivery of automated external exports.
/// This runs in the API "Hub", which has the necessary infrastructure 
/// (IExporterFactory) to communicate with external providers.
/// </summary>
public sealed class AutoExportConsumer(
    WinnowDbContext dbContext,
    IExporterFactory exporterFactory,
    ILogger<AutoExportConsumer> logger)
{
    public async Task Handle(ClusterAutoExportIntegrationEvent msg, CancellationToken ct)
    {

        // Find all active integrations with AutoExportEnabled for this project
        var integrations = await dbContext.Integrations
            .AsNoTracking()
            .Where(i => i.ProjectId == msg.ProjectId && i.IsActive && i.AutoExportEnabled)
            .ToListAsync(ct);

        if (integrations.Count == 0) return;

        foreach (var integration in integrations)
        {
            try
            {
                logger.LogInformation("Triggering automated export for Cluster {ClusterId} to {Provider}.", msg.ClusterId, integration.Provider);

                var exporter = await exporterFactory.GetExporterByIdAsync(integration.Id, ct);
                if (exporter is not null)
                {
                    var exportUrl = await exporter.ExportReportAsync(
                        $"[WINNOW] {msg.Title}",
                        $"{msg.Description}\n\n---\nView in Winnow: https://app.winnowtriage.com/clusters/{msg.ClusterId}",
                        ct);

                    logger.LogInformation("Automated export successful: {ExportUrl}", exportUrl);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to perform automated export for Cluster {ClusterId} to {Provider}.", msg.ClusterId, integration.Provider);
            }
        }
    }
}
