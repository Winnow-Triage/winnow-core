using FastEndpoints;
using MediatR;
using Svix;
using System.Text.Json;

namespace Winnow.API.Features.Webhooks.Resend;

public sealed class ResendWebhookEndpoint(
    IMediator mediator,
    IConfiguration config,
    ILogger<ResendWebhookEndpoint> logger) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Post("/webhooks/resend");
        AllowAnonymous();
        Options(x => x.RequireRateLimiting("webhook"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        try
        {
            // 1. Get raw request body for Svix verification
            using var reader = new StreamReader(HttpContext.Request.Body);
            var rawBody = await reader.ReadToEndAsync(ct);

            // 2. Extract Svix headers
            if (!HttpContext.Request.Headers.TryGetValue("svix-id", out var svixId) ||
                !HttpContext.Request.Headers.TryGetValue("svix-timestamp", out var svixTimestamp) ||
                !HttpContext.Request.Headers.TryGetValue("svix-signature", out var svixSignature))
            {
                logger.LogWarning("Missing required Svix headers in Resend webhook request.");
                await Send.UnauthorizedAsync(ct);
                return;
            }

            // 3. Verify Svix signature
            var secret = config["EmailSettings:Resend:WebhookSecret"];
            if (string.IsNullOrEmpty(secret))
            {
                logger.LogError("Resend Webhook Secret is not configured. Rejecting all webhooks.");
                await Send.ResponseAsync("Service Unavailable", 503, cancellation: ct);
                return;
            }

            try
            {
                var wh = new Webhook(secret);
                var headers = new System.Net.WebHeaderCollection
                {
                    { "svix-id", svixId.ToString() },
                    { "svix-timestamp", svixTimestamp.ToString() },
                    { "svix-signature", svixSignature.ToString() }
                };
                wh.Verify(rawBody, headers);
            }
            catch (Svix.Exceptions.WebhookVerificationException)
            {
                logger.LogWarning("Invalid Resend Webhook signature received.");
                await Send.UnauthorizedAsync(ct);
                return;
            }

            // 4. Parse payload and dispatch
            using var document = JsonDocument.Parse(rawBody);
            var root = document.RootElement;

            if (root.TryGetProperty("type", out var typeElem))
            {
                var eventType = typeElem.GetString() ?? string.Empty;
                var data = root.GetProperty("data");

                await mediator.Send(new ProcessResendWebhookCommand(eventType, data), ct);
                logger.LogInformation("Successfully processed Resend webhook event: {EventType}", eventType);
            }

            await Send.OkAsync(cancellation: ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fatal error processing Resend webhook.");
            await Send.ResponseAsync("Internal server error", 500, cancellation: ct);
        }
    }
}
