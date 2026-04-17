using Winnow.API.Infrastructure.Persistence;
using Winnow.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Winnow.Integrations.Domain;

namespace Winnow.API.Services.Discord;

public interface IClientNotificationService
{
    Task NotifyClusterCriticalAsync(Guid projectId, string clusterTitle, string clusterSummary);
    Task NotifyClusterVolumeMilestoneAsync(Guid projectId, Guid clusterId, int count, string? clusterTitle);
}

public class ClientNotificationService(
    WinnowDbContext dbContext,
    Wolverine.IMessageBus messageBus,
    Microsoft.Extensions.Configuration.IConfiguration config,
    ILogger<ClientNotificationService> logger)
    : IClientNotificationService
{
    private async Task<List<NotificationTarget>> GetNotificationTargetsAsync(Guid projectId)
    {
        var project = await dbContext.Projects
            .AsNoTracking()
            .Select(p => new { p.Id, p.OrganizationId })
            .FirstOrDefaultAsync(p => p.Id == projectId);

        if (project == null) return [];

        // Priority: Project-level integrations take precedence if specifically configured, 
        // but for notifications, we find all active ones that have notifications enabled.
        var integrations = await dbContext.Integrations
            .AsNoTracking()
            .Where(i => (i.ProjectId == projectId || (i.OrganizationId == project.OrganizationId && i.ProjectId == Guid.Empty))
                         && i.IsActive
                         && i.NotificationsEnabled)
            .ToListAsync();

        var targets = new List<NotificationTarget>();
        foreach (var integration in integrations)
        {
            if (integration.Config is DiscordConfig discordConfig && discordConfig.WebhookUrl != null)
                targets.Add(new NotificationTarget(discordConfig.WebhookUrl, null, NotificationProvider.Discord));
            else if (integration.Config is SlackConfig slackConfig && slackConfig.WebhookUrl != null)
                targets.Add(new NotificationTarget(slackConfig.WebhookUrl, null, NotificationProvider.Slack));
            else if (integration.Config is TeamsConfig teamsConfig && teamsConfig.WebhookUrl != null)
                targets.Add(new NotificationTarget(teamsConfig.WebhookUrl, null, NotificationProvider.MicrosoftTeams));
            else if (integration.Config is EmailConfig emailConfig && emailConfig.IsVerified && !string.IsNullOrWhiteSpace(emailConfig.RecipientEmail))
                targets.Add(new NotificationTarget(null, emailConfig.RecipientEmail, NotificationProvider.Email));
        }

        return targets;
    }

    private record NotificationTarget(Uri? WebhookUrl, string? EmailAddress, NotificationProvider Provider);

    public async Task NotifyClusterCriticalAsync(Guid projectId, string clusterTitle, string clusterSummary)
    {
        var targets = await GetNotificationTargetsAsync(projectId);

        if (targets.Count == 0)
        {
            logger.LogDebug("Client notification skipped for project {ProjectId}: No notification integrations configured.", projectId);
            return;
        }

        foreach (var target in targets)
        {
            await messageBus.PublishAsync(new SendWebhookNotificationCommand
            {
                WebhookUrl = target.WebhookUrl,
                RecipientAddress = target.EmailAddress,
                Provider = target.Provider,
                Title = $"⚠️ Critical Cluster Detected: {clusterTitle}",
                Message = clusterSummary,
                Color = "#FFA500", // Orange
                DetailUrl = new Uri($"{config["AppUrl"] ?? throw new InvalidOperationException("AppUrl configuration is missing.")}/projects/{projectId}/clusters")
            });
        }

        logger.LogInformation("Enqueued {Count} client notifications for high-criticality cluster in project {ProjectId}.", targets.Count, projectId);
    }

    public async Task NotifyClusterVolumeMilestoneAsync(Guid projectId, Guid clusterId, int count, string? clusterTitle)
    {
        var targets = await GetNotificationTargetsAsync(projectId);

        if (targets.Count == 0)
        {
            return;
        }

        var title = string.IsNullOrWhiteSpace(clusterTitle) ? "Unnamed Cluster" : clusterTitle;

        foreach (var target in targets)
        {
            await messageBus.PublishAsync(new SendWebhookNotificationCommand
            {
                WebhookUrl = target.WebhookUrl,
                RecipientAddress = target.EmailAddress,
                Provider = target.Provider,
                Title = $"📈 High Volume Cluster: {title}",
                Message = $"Cluster {clusterId} has reached {count} reports. This may indicate a widespread issue.",
                Color = "#3498db", // Blue
                DetailUrl = new Uri($"{config["AppUrl"] ?? throw new InvalidOperationException("AppUrl configuration is missing.")}/projects/{projectId}/clusters/{clusterId}")
            });
        }

        logger.LogInformation("Enqueued {Count} client notifications for high-volume cluster {ClusterId} in project {ProjectId}.", targets.Count, clusterId, projectId);
    }
}
