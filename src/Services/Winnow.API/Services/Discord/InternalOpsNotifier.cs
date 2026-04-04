using Microsoft.Extensions.Options;
using MassTransit;
using Winnow.API.Infrastructure.Configuration;
using Winnow.Contracts;
using Microsoft.Extensions.Logging;

namespace Winnow.API.Services.Discord;

public interface IInternalOpsNotifier
{
    Task NotifyNewSignupAsync(string userEmail);
    Task NotifyDlqAlertAsync(string queueName, string errorReason);
    Task NotifyStripePaymentAsync(string userEmail, decimal amount, string currency);
}

public class InternalOpsNotifier(
    IPublishEndpoint publishEndpoint,
    IOptions<DiscordOps> settings,
    ILogger<InternalOpsNotifier> logger)
    : IInternalOpsNotifier
{
    public async Task NotifyNewSignupAsync(string userEmail)
    {
        if (settings.Value.NewSignupsUrl == null) return;

        await publishEndpoint.Publish(new SendWebhookNotificationCommand
        {
            WebhookUrl = settings.Value.NewSignupsUrl,
            Provider = NotificationProvider.Discord,
            Title = "🚀 New Signup!",
            Message = $"A new user has signed up: **{userEmail}**.",
            Color = "#00FF00" // Green
        });

        logger.LogInformation("Enqueued Discord notification for new signup: {Email}", userEmail);
    }

    public async Task NotifyDlqAlertAsync(string queueName, string errorReason)
    {
        if (settings.Value.DlqAlertsUrl == null) return;

        await publishEndpoint.Publish(new SendWebhookNotificationCommand
        {
            WebhookUrl = settings.Value.DlqAlertsUrl,
            Provider = NotificationProvider.Discord,
            Title = "🚨 Dead Letter Queue Alert",
            Message = $"Message failed in queue **{queueName}**.\nReason: {errorReason}",
            Color = "#FF0000" // Red
        });

        logger.LogWarning("Enqueued Discord notification for DLQ alert on queue: {Queue}", queueName);
    }

    public async Task NotifyStripePaymentAsync(string userEmail, decimal amount, string currency)
    {
        if (settings.Value.StripePaymentsUrl == null) return;

        await publishEndpoint.Publish(new SendWebhookNotificationCommand
        {
            WebhookUrl = settings.Value.StripePaymentsUrl,
            Provider = NotificationProvider.Discord,
            Title = "💰 New Payment Received!",
            Message = $"Customer **{userEmail}** just paid **{amount} {currency.ToUpperInvariant()}**.",
            Color = "#85BB65" // Cash Green
        });

        logger.LogInformation("Enqueued Discord notification for Stripe payment: {Amount} {Currency} from {Email}",
            amount, currency, userEmail);
    }
}
