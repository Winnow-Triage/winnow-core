using MediatR;
using Microsoft.EntityFrameworkCore;
using Winnow.API.Domain.Clusters.Events;
using Winnow.API.Infrastructure.Persistence;
using Winnow.API.Services.Discord;
using MassTransit;
using Winnow.Contracts;

namespace Winnow.API.Features.Clusters.Events;

/// <summary>
/// Domain event handler for cluster-related events that triggers integration events for notifications.
/// This handler avoids direct notification delivery to remain safe for execution in worker nodes (e.g. Summary).
/// </summary>
public sealed class ClusterNotificationHandler(
    WinnowDbContext dbContext,
    IPublishEndpoint publishEndpoint,
    ILogger<ClusterNotificationHandler> logger)
    : INotificationHandler<ClusterReportAddedEvent>,
      INotificationHandler<ClusterSummarizedEvent>
{
    public async Task Handle(ClusterReportAddedEvent notification, CancellationToken ct)
    {
        // 1. Fetch the project and its organization's settings
        var project = await dbContext.Projects
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == notification.ProjectId, ct);

        if (project == null) return;

        var organization = await dbContext.Organizations
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == notification.OrganizationId, ct);

        if (organization == null) return;

        // 2. Resolve the volume threshold: Project Override ?? Organization Default
        var threshold = project.Notifications.VolumeThreshold
            ?? organization.Settings.Notifications.VolumeThreshold;

        // 3. Fetch the current cluster to check the report count
        var cluster = await dbContext.Clusters
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == notification.ClusterId, ct);

        if (cluster == null) return;

        // 4. Trigger only if the EXACT threshold is hit (avoids spam for every report thereafter)
        if (cluster.ReportCount == threshold)
        {
            logger.LogInformation("Cluster {ClusterId} reached volume threshold ({Count}). Publishing integration event.",
                cluster.Id, cluster.ReportCount);

            await publishEndpoint.Publish(new ClusterVolumeMilestoneReachedIntegrationEvent(
                project.Id,
                cluster.Id,
                cluster.ReportCount,
                cluster.Title ?? "Untitled Cluster"), ct);
        }
    }

    public async Task Handle(ClusterSummarizedEvent notification, CancellationToken ct)
    {
        // 1. Fetch the project and its organization's settings
        var project = await dbContext.Projects
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == notification.ProjectId, ct);

        if (project == null) return;

        var organization = await dbContext.Organizations
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == notification.OrganizationId, ct);

        if (organization == null) return;

        // 2. Resolve the criticality threshold: Project Override ?? Organization Default
        var threshold = project.Notifications.CriticalityThreshold
            ?? organization.Settings.Notifications.CriticalityThreshold;

        // 3. Trigger if the score meets or exceeds the threshold
        if (notification.CriticalityScore >= threshold)
        {
            logger.LogInformation("Cluster {ClusterId} reached criticality threshold ({Score} >= {Threshold}). Publishing integration event.",
                notification.ClusterId, notification.CriticalityScore, threshold);

            await publishEndpoint.Publish(new ClusterCriticalityThresholdReachedIntegrationEvent(
                project.Id,
                notification.Title,
                notification.Summary), ct);
        }
    }
}
