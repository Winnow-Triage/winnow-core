using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Domain.Organizations.ValueObjects;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Admin;

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
public sealed class UpdateOrganizationSubscriptionEndpoint(WinnowDbContext dbContext, Services.Quota.IQuotaService quotaService) : Endpoint<UpdateOrganizationSubscriptionRequest, UpdateOrganizationSubscriptionResponse>
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
        // Must ignore global query filters to see the organization regardless of tenant
        var organization = await dbContext.Organizations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(o => o.Id == req.Id, ct);

        if (organization == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        // Update fields
        var newPlan = SubscriptionPlan.FromName(req.SubscriptionTier);
        organization.ChangePlan(newPlan);
        await dbContext.SaveChangesAsync(ct);

        // Resolve any locked/overage discrepancies if the tier changed
        await quotaService.ResolveQuotaDiscrepanciesAsync(organization.Id, ct);

        var response = new UpdateOrganizationSubscriptionResponse
        {
            Id = organization.Id,
            Name = organization.Name,
            SubscriptionTier = organization.Plan.Name,
            StripeCustomerId = organization.BillingIdentity?.CustomerId,
            IsPaidTier = organization.Plan.TierLevel > 0,
            UpdatedAt = DateTime.UtcNow
        };

        await Send.OkAsync(response, ct);
    }
}