using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Stripe.BillingPortal;
using Winnow.Server.Infrastructure.MultiTenancy;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Billing;



public class PortalResponse
{
    public Uri PortalUrl { get; set; } = default!;
}

public class CreateCustomerPortalSessionEndpoint(
    WinnowDbContext db,
    IConfiguration config,
    ILogger<CreateCustomerPortalSessionEndpoint> logger,
    ITenantContext tenantContext)
    : EndpointWithoutRequest<PortalResponse>
{
    public override void Configure()
    {
        Post("/billing/portal");
        // Endpoint requires authentication by default since AllowAnonymous() is NOT called.
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!tenantContext.CurrentOrganizationId.HasValue)
        {
            HttpContext.Response.StatusCode = 400;
            return;
        }

        var organization = await db.Organizations
            .FirstOrDefaultAsync(o => o.Id == tenantContext.CurrentOrganizationId.Value, ct);

        if (organization == null)
        {
            await Send.NotFoundAsync(cancellation: ct);
            return;
        }

        if (string.IsNullOrEmpty(organization.StripeCustomerId))
        {
            logger.LogInformation("Creating new Stripe Customer for Organization {OrganizationId} directly from Portal Endpoint", organization.Id);

            var customerOptions = new Stripe.CustomerCreateOptions
            {
                Name = organization.Name,
                Metadata = new Dictionary<string, string>
                {
                    { "OrganizationId", organization.Id.ToString() }
                }
            };

            var customerService = new Stripe.CustomerService();
            var customer = await customerService.CreateAsync(customerOptions, cancellationToken: ct);

            organization.StripeCustomerId = customer.Id;
            await db.SaveChangesAsync(ct);
        }

        var frontendUrl = config["FrontendUrl"]?.TrimEnd('/') ?? "http://localhost:5173";
        var returnUrl = $"{frontendUrl}/settings?tab=billing";

        var options = new SessionCreateOptions
        {
            Customer = organization.StripeCustomerId,
            ReturnUrl = returnUrl,
        };

        var service = new Stripe.BillingPortal.SessionService();
        var session = await service.CreateAsync(options, cancellationToken: ct);

        await Send.OkAsync(new PortalResponse { PortalUrl = new Uri(session.Url) }, cancellation: ct);
    }
}
