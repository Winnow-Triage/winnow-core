using MediatR;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;
using Winnow.API.Domain.Organizations.ValueObjects;
using Winnow.API.Infrastructure.Billing;
using Winnow.API.Infrastructure.Persistence;

namespace Winnow.API.Features.Webhooks.ProcessStripe;

public record ProcessStripeWebhookCommand(Event StripeEvent) : IRequest<ProcessStripeWebhookResult>;

public record ProcessStripeWebhookResult(bool IsSuccess, string? ErrorMessage = null, int? StatusCode = null);

public class ProcessStripeWebhookHandler(
    WinnowDbContext db,
    IStripePlanMapper mapper,
    ILogger<ProcessStripeWebhookHandler> logger) : IRequestHandler<ProcessStripeWebhookCommand, ProcessStripeWebhookResult>
{
    public async Task<ProcessStripeWebhookResult> Handle(ProcessStripeWebhookCommand request, CancellationToken cancellationToken)
    {
        var stripeEvent = request.StripeEvent;

        try
        {
            switch (stripeEvent.Type)
            {
                case EventTypes.CheckoutSessionCompleted:
                    await HandleCheckoutSessionCompletedAsync(stripeEvent, cancellationToken);
                    break;
                case EventTypes.CustomerSubscriptionCreated:
                case EventTypes.CustomerSubscriptionUpdated:
                    await HandleSubscriptionUpdatedAsync(stripeEvent, cancellationToken);
                    break;
                case EventTypes.CustomerSubscriptionDeleted:
                    await HandleSubscriptionDeletedAsync(stripeEvent, cancellationToken);
                    break;
                default:
                    logger.LogInformation("Unhandled Stripe Webhook Event Type: {EventType}", stripeEvent.Type);
                    break;
            }

            return new ProcessStripeWebhookResult(true);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error processing Stripe Webhook event type {EventType}.", stripeEvent.Type);
            return new ProcessStripeWebhookResult(false, "Internal server error processing webhook.", 500);
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

        organization.LinkBillingIdentity(new BillingIdentity("Stripe", customerId, subscriptionId));

        try
        {
            var subscriptionService = new SubscriptionService();
            var subscription = await subscriptionService.GetAsync(subscriptionId, cancellationToken: ct);
            organization.ChangePlan(mapper.MapToDomainPlan(subscription));
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not fetch subscription {SubscriptionId} during checkout session parsing. Tier evaluation will rely on subscription events.", subscriptionId);
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Successfully processed checkout completed for Organization {OrganizationId}. Tier set to {Tier}.", organization.Id, organization.Plan.Name);
    }

    private async Task HandleSubscriptionUpdatedAsync(Event stripeEvent, CancellationToken ct)
    {
        if (stripeEvent.Data.Object is not Subscription subscription) return;

        var organization = await db.Organizations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(o => o.BillingIdentity.HasValue && o.BillingIdentity.Value.SubscriptionId == subscription.Id, ct);

        if (organization == null)
        {
            organization = await db.Organizations
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(o => o.BillingIdentity.HasValue && o.BillingIdentity.Value.CustomerId == subscription.CustomerId, ct);

            organization?.LinkBillingIdentity(new BillingIdentity("Stripe", subscription.CustomerId, subscription.Id));
        }

        if (organization == null)
        {
            logger.LogWarning("Organization not found for Stripe Subscription ID: {SubscriptionId} or Customer ID: {CustomerId}", subscription.Id, subscription.CustomerId);
            return;
        }

        organization.ChangePlan(mapper.MapToDomainPlan(subscription));

        logger.LogInformation("Updated Organization {OrganizationId} tier to {Tier} due to subscription status: {Status}",
            organization.Id, organization.Plan.Name, subscription.Status);

        await db.SaveChangesAsync(ct);
    }

    private async Task HandleSubscriptionDeletedAsync(Event stripeEvent, CancellationToken ct)
    {
        if (stripeEvent.Data.Object is not Subscription subscription) return;

        var organization = await db.Organizations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(o => o.BillingIdentity.HasValue && o.BillingIdentity.Value.SubscriptionId == subscription.Id, ct);

        if (organization == null)
        {
            logger.LogWarning("Organization not found for Stripe Subscription ID: {SubscriptionId}", subscription.Id);
            return;
        }

        organization.CancelSubscription();

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Downgraded Organization {OrganizationId} tier to Free and cleared SubscriptionId due to subscription deletion.", organization.Id);
    }
}
