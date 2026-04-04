using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net.Http.Json;
using MassTransit;
using Winnow.Contracts;

namespace Winnow.Notifications;

public class WebhookNotificationConsumer(
    IHttpClientFactory httpClientFactory,
    IEnumerable<IWebhookFormatter> formatters,
    ILogger<WebhookNotificationConsumer> logger)
    : IConsumer<SendWebhookNotificationCommand>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task Consume(ConsumeContext<SendWebhookNotificationCommand> context)
    {
        var command = context.Message;

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

                // Trigger MassTransit retry
                response.EnsureSuccessStatusCode();
            }

            logger.LogInformation("Successfully sent {Provider} notification to webhook.", command.Provider);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send {Provider} notification.",
                command.Provider);
            throw; // Re-throw to allow MassTransit to retry
        }
    }
}
