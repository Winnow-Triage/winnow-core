using Winnow.API.Infrastructure.Persistence;
using Winnow.API.Domain.Projects;
using MassTransit;
using Winnow.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Winnow.API.Services.Discord;

public interface IClientNotificationService
{
    Task NotifyClusterCriticalAsync(Guid projectId, string clusterTitle, string clusterSummary);
}

public class ClientNotificationService(
    WinnowDbContext dbContext,
    IPublishEndpoint publishEndpoint,
    ILogger<ClientNotificationService> logger)
    : IClientNotificationService
{
    public async Task NotifyClusterCriticalAsync(Guid projectId, string clusterTitle, string clusterSummary)
    {
        var project = await dbContext.Projects
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == projectId);

        if (project == null || project.DiscordWebhookUrl == null)
        {
            logger.LogDebug("Client notification skipped for project {ProjectId}: No Discord webhook configured.", projectId);
            return;
        }

        await publishEndpoint.Publish(new SendWebhookNotificationCommand
        {
            WebhookUrl = project.DiscordWebhookUrl,
            Provider = NotificationProvider.Discord, // Default to Discord for now
            Title = $"⚠️ Critical Cluster Detected: {clusterTitle}",
            Message = clusterSummary,
            Color = "#FFA500", // Orange
            DetailUrl = new Uri($"https://app.winnowtriage.com/projects/{projectId}/clusters") // Dashboard URL
        });

        logger.LogInformation("Enqueued client Discord notification for project {ProjectId}.", projectId);
    }
}
