using FastEndpoints;
using MediatR;

namespace Winnow.Server.Features.Admin.Organizations.UpdateSubscription;

/// <summary>
/// Request DTO for updating an organization's subscription tier.
/// </summary>
public class UpdateOrganizationSubscriptionRequest
{
    public Guid Id { get; set; }
    public string SubscriptionTier { get; set; } = string.Empty;
    public string? StripeCustomerId { get; set; } // optional update
}

/// <summary>
/// Response DTO after updating subscription.
/// </summary>
public class UpdateOrganizationSubscriptionResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SubscriptionTier { get; set; } = string.Empty;
    public string? StripeCustomerId { get; set; }
    public bool IsPaidTier { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Admin endpoint to manually override an organization's subscription tier.
/// </summary>
public sealed class UpdateOrganizationSubscriptionEndpoint(IMediator mediator) : Endpoint<UpdateOrganizationSubscriptionRequest, UpdateOrganizationSubscriptionResponse>
{
    public override void Configure()
    {
        Post("/admin/organizations/{id}/subscription");
        Roles("SuperAdmin");
        Summary(s =>
        {
            s.Summary = "Update organization subscription tier (SuperAdmin only)";
            s.Description = "Manually override a tenant's subscription tier and optionally Stripe customer ID, bypassing tenant isolation.";
            s.Response<UpdateOrganizationSubscriptionResponse>(200, "Success");
            s.Response(400, "Invalid subscription tier");
            s.Response(404, "Organization not found");
            s.Response(401, "Unauthorized (missing or invalid JWT)");
            s.Response(403, "Forbidden (user is not SuperAdmin)");
        });
    }

    public override async Task HandleAsync(UpdateOrganizationSubscriptionRequest req, CancellationToken ct)
    {
        var command = new UpdateOrganizationSubscriptionCommand
        {
            Id = req.Id,
            SubscriptionTier = req.SubscriptionTier,
            StripeCustomerId = req.StripeCustomerId
        };

        try
        {
            var result = await mediator.Send(command, ct);
            await Send.OkAsync(result, ct);
        }
        catch (InvalidOperationException)
        {
            await Send.NotFoundAsync(ct);
        }
    }
}