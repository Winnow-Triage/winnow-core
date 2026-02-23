using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;
using Winnow.Server.Infrastructure.MultiTenancy;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Billing;

public class CheckoutRequest
{
    public string TargetTier { get; set; } = string.Empty;
}

public class CheckoutResponse
{
    public Uri CheckoutUrl { get; set; } = default!;
}

public class CreateCheckoutSessionEndpoint(
    WinnowDbContext db,
    IConfiguration config,
    ILogger<CreateCheckoutSessionEndpoint> logger,
    ITenantContext tenantContext)
    : Endpoint<CheckoutRequest, CheckoutResponse>
{
    public override void Configure()
    {
        Post("/billing/checkout");
        // Endpoint requires authentication by default since AllowAnonymous() is NOT called.
    }

    public override async Task HandleAsync(CheckoutRequest req, CancellationToken ct)
    {
        if (!tenantContext.CurrentOrganizationId.HasValue)
        {
            await Send.ErrorsAsync(400, cancellation: ct);
            return;
        }

        var organization = await db.Organizations
            .FirstOrDefaultAsync(o => o.Id == tenantContext.CurrentOrganizationId.Value, ct);

        if (organization == null)
        {
            await Send.NotFoundAsync(cancellation: ct);
            return;
        }

        // 1. Create a new Stripe Customer if one does not exist for this Organization
        if (string.IsNullOrEmpty(organization.StripeCustomerId))
        {
            logger.LogInformation("Creating new Stripe Customer for Organization {OrganizationId}", organization.Id);

            var customerOptions = new CustomerCreateOptions
            {
                Name = organization.Name,
                Metadata = new Dictionary<string, string>
                {
                    { "OrganizationId", organization.Id.ToString() }
                }
            };

            var customerService = new CustomerService();
            var customer = await customerService.CreateAsync(customerOptions, cancellationToken: ct);

            organization.StripeCustomerId = customer.Id;
            await db.SaveChangesAsync(ct);

            logger.LogInformation("Successfully created Stripe Customer {CustomerId}", customer.Id);
        }

        // 2. Map Target Tier to the actual Stripe Price ID from appsettings.json
        var priceId = req.TargetTier switch
        {
            "Starter" => config["Stripe:Prices:Starter"],
            "Pro" => config["Stripe:Prices:Pro"],
            "Enterprise" => config["Stripe:Prices:Enterprise"],
            _ => null
        };

        if (string.IsNullOrEmpty(priceId))
        {
            logger.LogWarning("Invalid TargetTier requested: {TargetTier}", req.TargetTier);
            AddError(r => r.TargetTier, $"Invalid or unconfigured target tier: {req.TargetTier}");
            await Send.ResponseAsync(new CheckoutResponse { }, 400, cancellation: ct);
            return;
        }

        // 3. Create Stripe Checkout Session
        var frontendUrl = config["FrontendUrl"]?.TrimEnd('/') ?? "http://localhost:5173";
        var billingUrl = $"{frontendUrl}/settings?tab=billing";

        var sessionOptions = new SessionCreateOptions
        {
            Mode = "subscription",
            Customer = organization.StripeCustomerId,
            LineItems = [
                new SessionLineItemOptions
                {
                    Price = priceId,
                    Quantity = 1,
                },
            ],
            SuccessUrl = billingUrl,
            CancelUrl = billingUrl,
        };

        var sessionService = new SessionService();
        var session = await sessionService.CreateAsync(sessionOptions, cancellationToken: ct);

        // 4. Return Session URL to frontend
        await Send.OkAsync(new CheckoutResponse { CheckoutUrl = new Uri(session.Url) }, cancellation: ct);
    }
}
