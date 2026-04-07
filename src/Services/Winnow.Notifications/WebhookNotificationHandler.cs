using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net.Http.Json;
using Winnow.Contracts;

namespace Winnow.Notifications;

public class WebhookNotificationHandler(
    IHttpClientFactory httpClientFactory,
    IEnumerable<IWebhookFormatter> formatters,
    Winnow.API.Services.Emails.IEmailService emailService,
    ILogger<WebhookNotificationHandler> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task Handle(SendWebhookNotificationCommand command)
    {
        if (command.Provider == NotificationProvider.Email)
        {
            if (string.IsNullOrWhiteSpace(command.RecipientAddress))
            {
                logger.LogWarning("Discarding email notification: RecipientAddress is null.");
                return;
            }

            try
            {
                var htmlBody = $"<h2>{command.Title}</h2><p>{command.Message}</p>";
                if (command.DetailUrl != null)
                {
                    htmlBody += $"<p><a href=\"{command.DetailUrl}\">View Details</a></p>";
                }

                await emailService.SendEmailAsync(
                    command.RecipientAddress,
                    command.Title ?? "Winnow Notification",
                    htmlBody);

                logger.LogInformation("Successfully sent email notification to {Target}.", command.RecipientAddress);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send email notification to {Target}.", command.RecipientAddress);
                throw;
            }
            return;
        }

        if (command.WebhookUrl == null)
        {
            logger.LogWarning("Discarding notification: WebhookUrl is null.");
            return;
        }

        var formatter = formatters.FirstOrDefault(f => f.Provider == command.Provider);
        if (formatter == null)
        {
            logger.LogError("No formatter found for provider {Provider}. Discarding notification.", command.Provider);
            return;
        }

        try
        {
            var payload = formatter.Format(command);
            var client = httpClientFactory.CreateClient("Webhooks");

            var response = await client.PostAsJsonAsync(command.WebhookUrl, payload, JsonOptions);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                logger.LogError("Webhook API for {Provider} returned {StatusCode}: {Error}",
                    command.Provider, response.StatusCode, errorBody);

                // Trigger Wolverine retry by throwing exception
                response.EnsureSuccessStatusCode();
            }

            logger.LogInformation("Successfully sent {Provider} notification to webhook.", command.Provider);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send {Provider} notification.",
                command.Provider);
            throw; // Re-throw to allow Wolverine to retry
        }
    }
}
