using MediatR;
using Microsoft.EntityFrameworkCore;
using Winnow.API.Domain.Clusters.Events;
using Winnow.API.Infrastructure.Persistence;
using Winnow.Contracts;

namespace Winnow.API.Features.Clusters.Events;

/// <summary>
/// Domain event handler for cluster-related events that trigger automated exports to external integrations.
/// </summary>
public sealed class AutoExportHandler(
    WinnowDbContext dbContext,
    Wolverine.IMessageBus messageBus,
    ILogger<AutoExportHandler> logger)
    : INotificationHandler<ClusterReportAddedEvent>,
      INotificationHandler<ClusterSummarizedEvent>
{
    public async Task Handle(ClusterReportAddedEvent notification, CancellationToken cancellationToken)
    {
        // 1. Fetch project and organization settings
        var project = await dbContext.Projects
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == notification.ProjectId, cancellationToken);

        if (project == null) return;

        var organization = await dbContext.Organizations
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == notification.OrganizationId, cancellationToken);

        if (organization == null) return;

        // 2. Resolve thresholds
        var threshold = project.Notifications.VolumeThreshold
            ?? organization.Settings.Notifications.VolumeThreshold;

        // 3. Check if exact threshold was hit
        var cluster = await dbContext.Clusters
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == notification.ClusterId, cancellationToken);

        if (cluster == null || cluster.ReportCount != threshold) return;

        // 4. Trigger auto-exports for active integrations
        await TriggerAutoExportsAsync(project.Id, cluster.Id, cluster.Title ?? "Untitled Cluster", cluster.Summary ?? "Automatic export due to volume milestone.");
    }

    public async Task Handle(ClusterSummarizedEvent notification, CancellationToken cancellationToken)
    {
        // 1. Fetch project and organization settings
        var project = await dbContext.Projects
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == notification.ProjectId, cancellationToken);

        if (project == null) return;

        var organization = await dbContext.Organizations
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == notification.OrganizationId, cancellationToken);

        if (organization == null) return;

        // 2. Resolve thresholds
        var threshold = project.Notifications.CriticalityThreshold
            ?? organization.Settings.Notifications.CriticalityThreshold;

        // 3. Check if threshold met
        if (notification.CriticalityScore < threshold) return;

        // 5. Trigger auto-exports
        await TriggerAutoExportsAsync(project.Id, notification.ClusterId, notification.Title, notification.Summary);
    }

    private async Task TriggerAutoExportsAsync(Guid projectId, Guid clusterId, string title, string description)
    {
        // Offload the cross-boundary export logic to the API "Hub" using Wolverine
        logger.LogInformation("Enqueuing auto-export integration event for Cluster {ClusterId}.", clusterId);

        await messageBus.PublishAsync(new ClusterAutoExportIntegrationEvent(
            projectId,
            clusterId,
            title,
            description
        ));
    }
}
