using FastEndpoints;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Stripe;
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
                case EventTypes.CustomerSubscriptionCreated:
                case EventTypes.CustomerSubscriptionUpdated:
                    await HandleSubscriptionUpsertAsync(stripeEvent, ct);
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

    private async Task HandleSubscriptionUpsertAsync(Event stripeEvent, CancellationToken ct)
    {
        if (stripeEvent.Data.Object is not Subscription subscription) return;

        var organization = await db.Organizations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(o => o.StripeCustomerId == subscription.CustomerId, ct);

        if (organization == null)
        {
            logger.LogInformation("Organization not found for Stripe Customer ID: {CustomerId}. Creating new Organization.", subscription.CustomerId);

            organization = new Winnow.Server.Entities.Organization
            {
                Name = $"Stripe Customer {subscription.CustomerId}", // Placeholder name until user claims/updates it
                StripeCustomerId = subscription.CustomerId,
                StripeSubscriptionId = subscription.Id,
                SubscriptionTier = "Free",
                CreatedAt = DateTime.UtcNow
            };

            db.Organizations.Add(organization);

            // Create a Default Project for the new organization
            var projectId = Guid.NewGuid();
            var apiKeyService = HttpContext.RequestServices.GetRequiredService<Winnow.Server.Infrastructure.Security.IApiKeyService>();
            var plaintextKey = apiKeyService.GeneratePlaintextKey(projectId);
            var project = new Project
            {
                Id = projectId,
                Name = "Default Project",
                OwnerId = "", // Owner not yet known
                OrganizationId = organization.Id,
                ApiKeyHash = apiKeyService.HashKey(plaintextKey)
            };
            db.Projects.Add(project);
        }

        if (subscription.Status == "active" || subscription.Status == "trialing")
        {
            var priceId = subscription.Items.Data.FirstOrDefault()?.Price.Id;
            var tierString = priceId switch
            {
                var id when id == config["Stripe:Prices:Starter"] => "Starter",
                var id when id == config["Stripe:Prices:Pro"] => "Pro",
                var id when id == config["Stripe:Prices:Enterprise"] => "Enterprise",
                _ => "Free"
            };

            organization.SubscriptionTier = tierString;
            organization.StripeSubscriptionId = subscription.Id;
            logger.LogInformation("Updated Organization {OrganizationId} tier to {Tier}", organization.Id, tierString);
        }
        else
        {
            organization.SubscriptionTier = "Free";
            organization.StripeSubscriptionId = subscription.Id;
            logger.LogInformation("Downgraded Organization {OrganizationId} tier to Free due to status: {Status}", organization.Id, subscription.Status);
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task HandleSubscriptionDeletedAsync(Event stripeEvent, CancellationToken ct)
    {
        if (stripeEvent.Data.Object is not Subscription subscription) return;

        var organization = await db.Organizations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(o => o.StripeCustomerId == subscription.CustomerId, ct);

        if (organization == null)
        {
            logger.LogWarning("Organization not found for Stripe Customer ID: {CustomerId}", subscription.CustomerId);
            return;
        }

        organization.SubscriptionTier = "Free";
        organization.StripeSubscriptionId = null;
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Downgraded Organization {OrganizationId} tier to Free due to subscription deletion.", organization.Id);
    }
}
