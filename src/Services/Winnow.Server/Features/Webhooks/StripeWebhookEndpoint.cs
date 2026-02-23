using FastEndpoints;
using Microsoft.AspNetCore.Authorization;
using Stripe;

namespace Winnow.Server.Features.Webhooks;

[AllowAnonymous]
public class StripeWebhookEndpoint(IConfiguration config) : EndpointWithoutRequest
{
    private readonly IConfiguration _config = config;

    public override void Configure()
    {
        Post("/api/webhooks/stripe");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync(ct);
        var signature = HttpContext.Request.Headers["Stripe-Signature"];
        var endpointSecret = _config["Stripe:WebhookSecret"];

        if (string.IsNullOrEmpty(endpointSecret))
        {
            ThrowError("Stripe:WebhookSecret is not configured.");
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

            switch (stripeEvent.Type)
            {
                case EventTypes.CustomerSubscriptionUpdated:
                    var subscription = stripeEvent.Data.Object as Subscription;
                    // TODO: Inject DbContext and write specific database update logic later
                    break;
                default:
                    // Unhandled event type
                    break;
            }

            await Send.OkAsync(ct);
        }
        catch (StripeException)
        {
            await Send.ResponseAsync("Invalid signature", 400, cancellation: ct);
        }
    }
}
