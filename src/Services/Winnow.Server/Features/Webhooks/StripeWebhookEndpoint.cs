using FastEndpoints;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;
using Winnow.Server.Entities;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Webhooks;

[AllowAnonymous]
public sealed class StripeWebhookEndpoint(IConfiguration config, WinnowDbContext db, ILogger<StripeWebhookEndpoint> logger) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Post("/api/webhooks/stripe");
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

            switch (stripeEvent.Type)
            {
                case EventTypes.CheckoutSessionCompleted:
                    await HandleCheckoutSessionCompletedAsync(stripeEvent, ct);
                    break;
                case EventTypes.CustomerSubscriptionCreated:
                case EventTypes.CustomerSubscriptionUpdated:
                    await HandleSubscriptionUpdatedAsync(stripeEvent, ct);
                    break;
                case EventTypes.CustomerSubscriptionDeleted:
                    await HandleSubscriptionDeletedAsync(stripeEvent, ct);
                    break;
                default:
                    logger.LogInformation("Unhandled Stripe Webhook Event Type: {EventType}", stripeEvent.Type);
                    break;
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

    private async Task HandleCheckoutSessionCompletedAsync(Event stripeEvent, CancellationToken ct)
    {
        if (stripeEvent.Data.Object is not Session session) return;

        var subscriptionId = session.SubscriptionId;
        var customerId = session.CustomerId;

        var orgIdString = session.ClientReferenceId;
        if (string.IsNullOrEmpty(orgIdString) && session.Metadata != null && session.Metadata.TryGetValue("OrganizationId", out var metaOrgId))
        {
            orgIdString = metaOrgId;
        }

        if (string.IsNullOrEmpty(orgIdString) || !Guid.TryParse(orgIdString, out var organizationId))
        {
            logger.LogWarning("OrganizationId could not be extracted from Checkout Session {SessionId}", session.Id);
            return;
        }

        var organization = await db.Organizations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(o => o.Id == organizationId, ct);

        if (organization == null)
        {
            logger.LogWarning("Organization {OrganizationId} not found in database.", organizationId);
            return;
        }

        organization.StripeCustomerId = customerId;
        organization.StripeSubscriptionId = subscriptionId;

        try
        {
            var subscriptionService = new SubscriptionService();
            var subscription = await subscriptionService.GetAsync(subscriptionId, cancellationToken: ct);
            organization.SubscriptionTier = GetTierFromSubscription(subscription);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not fetch subscription {SubscriptionId} during checkout session parsing. Tier evaluation will rely on subscription events.", subscriptionId);
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Successfully processed checkout completed for Organization {OrganizationId}. Tier set to {Tier}.", organization.Id, organization.SubscriptionTier);
    }

    private async Task HandleSubscriptionUpdatedAsync(Event stripeEvent, CancellationToken ct)
    {
        if (stripeEvent.Data.Object is not Subscription subscription) return;

        var organization = await db.Organizations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(o => o.StripeSubscriptionId == subscription.Id, ct);

        if (organization == null)
        {
            // If matching by SubscriptionId fails, try matching by CustomerId fallback 
            // (Checkout session might not have completed yet)
            organization = await db.Organizations
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(o => o.StripeCustomerId == subscription.CustomerId, ct);

            if (organization != null)
            {
                organization.StripeSubscriptionId = subscription.Id;
            }
        }

        if (organization == null)
        {
            logger.LogWarning("Organization not found for Stripe Subscription ID: {SubscriptionId} or Customer ID: {CustomerId}", subscription.Id, subscription.CustomerId);
            return;
        }

        organization.SubscriptionTier = GetTierFromSubscription(subscription);
        logger.LogInformation("Updated Organization {OrganizationId} tier to {Tier} due to subscription status: {Status}", organization.Id, organization.SubscriptionTier, subscription.Status);

        await db.SaveChangesAsync(ct);
    }

    private async Task HandleSubscriptionDeletedAsync(Event stripeEvent, CancellationToken ct)
    {
        if (stripeEvent.Data.Object is not Subscription subscription) return;

        var organization = await db.Organizations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(o => o.StripeSubscriptionId == subscription.Id, ct);

        if (organization == null)
        {
            logger.LogWarning("Organization not found for Stripe Subscription ID: {SubscriptionId}", subscription.Id);
            return;
        }

        organization.SubscriptionTier = "Free";
        organization.StripeSubscriptionId = null;

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Downgraded Organization {OrganizationId} tier to Free and cleared SubscriptionId due to subscription deletion.", organization.Id);
    }

    private string GetTierFromSubscription(Subscription subscription)
    {
        if (subscription == null || (subscription.Status != "active" && subscription.Status != "trialing"))
        {
            return "Free";
        }

        var priceId = subscription.Items?.Data?.FirstOrDefault()?.Price?.Id;
        return priceId switch
        {
            var id when id == config["Stripe:Prices:Starter"] => "Starter",
            var id when id == config["Stripe:Prices:Pro"] => "Pro",
            var id when id == config["Stripe:Prices:Enterprise"] => "Enterprise",
            _ => "Free"
        };
    }
}
