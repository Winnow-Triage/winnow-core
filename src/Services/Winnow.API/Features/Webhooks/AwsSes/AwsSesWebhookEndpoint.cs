using FastEndpoints;
using MediatR;
using System.Text.Json;

namespace Winnow.API.Features.Webhooks.AwsSes;

public class SnsPayload
{
    public string Type { get; set; } = string.Empty;
    public string MessageId { get; set; } = string.Empty;
    public string TopicArn { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
#pragma warning disable CA1056
    public string SubscribeURL { get; set; } = string.Empty;
#pragma warning restore CA1056
}

public sealed class AwsSesWebhookEndpoint(IMediator mediator, IHttpClientFactory httpClientFactory, ILogger<AwsSesWebhookEndpoint> logger) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Post("/api/webhooks/ses");
        AllowAnonymous();
        Options(x => x.RequireRateLimiting("webhook"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        try
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync(ct);
            var payload = JsonSerializer.Deserialize<SnsPayload>(json);

            if (payload == null)
            {
                await Send.OkAsync(cancellation: ct);
                return;
            }

            if (payload.Type == "SubscriptionConfirmation" && !string.IsNullOrEmpty(payload.SubscribeURL))
            {
                logger.LogInformation("Received SNS SubscriptionConfirmation. Visiting SubscribeURL.");
                var client = httpClientFactory.CreateClient();
                await client.GetAsync(payload.SubscribeURL, ct);
                await Send.OkAsync(cancellation: ct);
                return;
            }

            if (payload.Type == "Notification")
            {
                // Message contains the payload from SES
                var bounceCommand = new ProcessSesBounceCommand(payload.Message);
                await mediator.Send(bounceCommand, ct);
            }

            await Send.OkAsync(cancellation: ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing AWS SES SNS Webhook.");
            await Send.ResponseAsync("Internal server error", 500, cancellation: ct);
        }
    }
}
