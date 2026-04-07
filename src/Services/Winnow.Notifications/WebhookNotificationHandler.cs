using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net.Http.Json;
using Winnow.Contracts;

namespace Winnow.Notifications;

public class WebhookNotificationHandler(
    IHttpClientFactory httpClientFactory,
    IEnumerable<IWebhookFormatter> formatters,
    ILogger<WebhookNotificationHandler> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task Handle(SendWebhookNotificationCommand command)
    {
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
