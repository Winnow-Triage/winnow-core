using MediatR;
using Winnow.API.Infrastructure.Security.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe;
using Stripe.Checkout;
using Winnow.API.Infrastructure.Persistence;
using Winnow.API.Domain.Organizations.ValueObjects;
using Winnow.API.Features.Shared;

namespace Winnow.API.Features.Billing.Create;

[RequirePermission("billing:manage")]
public record CreateCheckoutSessionCommand(Guid CurrentOrganizationId, string TargetTier) : IRequest<CreateCheckoutSessionResult>, IOrgScopedRequest;

public record CreateCheckoutSessionResult(bool IsSuccess, Uri? CheckoutUrl, string? ErrorMessage = null, int? StatusCode = null);

public class CreateCheckoutSessionHandler(
    WinnowDbContext db,
    IConfiguration config,
    ILogger<CreateCheckoutSessionHandler> logger) : IRequestHandler<CreateCheckoutSessionCommand, CreateCheckoutSessionResult>
{
    public async Task<CreateCheckoutSessionResult> Handle(CreateCheckoutSessionCommand request, CancellationToken cancellationToken)
    {
        var organization = await db.Organizations
            .FirstOrDefaultAsync(o => o.Id == request.CurrentOrganizationId, cancellationToken);

        if (organization == null)
        {
            return new CreateCheckoutSessionResult(false, null, "Organization not found", 404);
        }

        // 1. Create a new Stripe Customer if one does not exist for this Organization
        if (organization.BillingIdentity == null || organization.BillingIdentity.Value.Provider != "Stripe")
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
            var customer = await customerService.CreateAsync(customerOptions, cancellationToken: cancellationToken);

            organization.LinkBillingIdentity(new BillingIdentity("Stripe", customer.Id, null));
            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Successfully created Stripe Customer {CustomerId}", customer.Id);
        }

        // 2. Map Target Tier to the actual Stripe Price ID from appsettings.json
        var priceId = request.TargetTier switch
        {
            "Starter" => config["Stripe:Prices:Starter"],
            "Pro" => config["Stripe:Prices:Pro"],
            "Enterprise" => config["Stripe:Prices:Enterprise"],
            _ => null
        };

        if (string.IsNullOrEmpty(priceId))
        {
            logger.LogWarning("Invalid TargetTier requested: {TargetTier}", request.TargetTier);
            return new CreateCheckoutSessionResult(false, null, $"Invalid or unconfigured target tier: {request.TargetTier}", 400);
        }

        // 3. Create Stripe Checkout Session
        var frontendUrl = config["FrontendUrl"]?.TrimEnd('/') ?? "http://localhost:5173";
        var billingUrl = $"{frontendUrl}/settings?tab=billing";

        var sessionOptions = new SessionCreateOptions
        {
            Mode = "subscription",
            Customer = organization.BillingIdentity!.Value.CustomerId,
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
        var session = await sessionService.CreateAsync(sessionOptions, cancellationToken: cancellationToken);

        return new CreateCheckoutSessionResult(true, new Uri(session.Url));
    }
}
