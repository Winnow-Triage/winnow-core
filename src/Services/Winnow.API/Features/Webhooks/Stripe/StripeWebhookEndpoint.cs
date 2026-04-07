using FastEndpoints;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Stripe;
using Winnow.API.Features.Webhooks.ProcessStripe;

namespace Winnow.API.Features.Webhooks.Stripe;

[AllowAnonymous]
public sealed class StripeWebhookEndpoint(IConfiguration config, IMediator mediator, ILogger<StripeWebhookEndpoint> logger) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Post("/webhooks/stripe");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync(ct);
        var signature = HttpContext.Request.Headers["Stripe-Signature"];
        var endpointSecret = config["Stripe:WebhookSecret"];

        if (string.IsNullOrEmpty(endpointSecret))
        {
            logger.LogError("Stripe:WebhookSecret is not configured.");
            ThrowError("Stripe:WebhookSecret is not configured.");
            return;
        }

        try
        {
            var stripeEvent = EventUtility.ConstructEvent(
                json,
                signature,
                endpointSecret,
                300,
                true
            );

            logger.LogInformation("Processing Stripe Webhook Event: {EventType}", stripeEvent.Type);

            var command = new ProcessStripeWebhookCommand(stripeEvent);
            var result = await mediator.Send(command, ct);

            if (!result.IsSuccess)
            {
                await Send.ResponseAsync(result.ErrorMessage ?? "Internal server error processing webhook.", result.StatusCode ?? 500, cancellation: ct);
                return;
            }

            await Send.OkAsync(cancellation: ct);
        }
        catch (StripeException e)
        {
            logger.LogWarning(e, "Invalid Stripe Signature.");
            await Send.ResponseAsync("Invalid signature", 400, cancellation: ct);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error processing Stripe Webhook.");
            await Send.ResponseAsync("Internal server error processing webhook.", 500, cancellation: ct);
        }
    }
}
