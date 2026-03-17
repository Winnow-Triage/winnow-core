using MediatR;
using Winnow.Server.Infrastructure.Security.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe.BillingPortal;
using Winnow.Server.Domain.Organizations.ValueObjects;
using Winnow.Server.Infrastructure.Persistence;
using Winnow.Server.Features.Shared;

namespace Winnow.Server.Features.Billing.Create;

[RequirePermission("billing:manage")]
public record CreateCustomerPortalSessionCommand(Guid CurrentOrganizationId, string? Action) : IRequest<CreateCustomerPortalSessionResult>, IOrgScopedRequest;

public record CreateCustomerPortalSessionResult(bool IsSuccess, Uri? PortalUrl, string? ErrorMessage = null, int? StatusCode = null);

public class CreateCustomerPortalSessionHandler(
    WinnowDbContext db,
    IConfiguration config,
    ILogger<CreateCustomerPortalSessionHandler> logger) : IRequestHandler<CreateCustomerPortalSessionCommand, CreateCustomerPortalSessionResult>
{
    public async Task<CreateCustomerPortalSessionResult> Handle(CreateCustomerPortalSessionCommand request, CancellationToken cancellationToken)
    {
        var organization = await db.Organizations
            .FirstOrDefaultAsync(o => o.Id == request.CurrentOrganizationId, cancellationToken);

        if (organization == null)
        {
            return new CreateCustomerPortalSessionResult(false, null, "Organization not found", 404);
        }

        var customerId = organization.BillingIdentity?.CustomerId;

        if (string.IsNullOrEmpty(customerId))
        {
            logger.LogInformation("Creating new Stripe Customer for Organization {OrganizationId} directly from Portal Endpoint", organization.Id);

            var customerOptions = new Stripe.CustomerCreateOptions
            {
                Name = organization.Name,
                Email = organization.ContactEmail.Value,
                Metadata = new Dictionary<string, string>
                {
                    { "OrganizationId", organization.Id.ToString() }
                }
            };

            var customerService = new Stripe.CustomerService();
            var customer = await customerService.CreateAsync(customerOptions, cancellationToken: cancellationToken);

            customerId = customer.Id;

            organization.LinkBillingIdentity(new BillingIdentity("Stripe", customerId, null));
            await db.SaveChangesAsync(cancellationToken);
        }

        var frontendUrl = config["FrontendUrl"]?.TrimEnd('/') ?? "http://localhost:5173";
        var returnUrl = $"{frontendUrl}/settings?tab=billing";

        var options = new SessionCreateOptions
        {
            Customer = customerId,
            ReturnUrl = returnUrl,
        };

        var subscriptionId = organization.BillingIdentity?.SubscriptionId;

        if (!string.IsNullOrEmpty(subscriptionId) && !string.IsNullOrEmpty(request.Action))
        {
            options.FlowData = request.Action.ToLowerInvariant() switch
            {
                "update" => new SessionFlowDataOptions
                {
                    Type = "subscription_update",
                    SubscriptionUpdate = new SessionFlowDataSubscriptionUpdateOptions
                    {
                        Subscription = subscriptionId,
                    },
                },
                "cancel" => new SessionFlowDataOptions
                {
                    Type = "subscription_cancel",
                    SubscriptionCancel = new SessionFlowDataSubscriptionCancelOptions
                    {
                        Subscription = subscriptionId,
                    },
                },
                _ => null,
            };
        }

        var service = new Stripe.BillingPortal.SessionService();
        var session = await service.CreateAsync(options, cancellationToken: cancellationToken);

        return new CreateCustomerPortalSessionResult(true, new Uri(session.Url));
    }
}
