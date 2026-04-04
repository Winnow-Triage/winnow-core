using Winnow.API.Services.Discord;
using Winnow.Contracts;

namespace Winnow.API.Features.Clusters.Events;

/// <summary>
/// Wolverine message handler that handles the final delivery of cluster notifications.
/// This runs in the API "Hub", which has the necessary infrastructure 
/// (IClientNotificationService) to send alerts.
/// </summary>
public sealed class ClusterNotificationConsumer(
    IClientNotificationService notificationService,
    ILogger<ClusterNotificationConsumer> logger)
{
    public async Task Handle(ClusterVolumeMilestoneReachedIntegrationEvent msg)
    {
        logger.LogInformation("Processing volume milestone notification for Cluster {ClusterId} (Count: {Count})",
            msg.ClusterId, msg.ReportCount);

        await notificationService.NotifyClusterVolumeMilestoneAsync(
            msg.ProjectId,
            msg.ClusterId,
            msg.ReportCount,
            msg.Title);
    }

    public async Task Handle(ClusterCriticalityThresholdReachedIntegrationEvent msg)
    {
        logger.LogInformation("Processing criticality threshold notification for Project {ProjectId} (Title: {Title})",
            msg.ProjectId, msg.Title);

        await notificationService.NotifyClusterCriticalAsync(
            msg.ProjectId,
            msg.Title,
            msg.Summary);
    }
}
