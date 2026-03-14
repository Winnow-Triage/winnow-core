using MediatR;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Domain.Organizations.ValueObjects;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Admin.Organizations.UpdateSubscription;

public record UpdateOrganizationSubscriptionCommand : IRequest<UpdateOrganizationSubscriptionResponse>
{
    public Guid Id { get; init; }
    public string SubscriptionTier { get; init; } = string.Empty;
    public string? StripeCustomerId { get; init; } // optional update
}

public class UpdateOrganizationSubscriptionHandler(
    WinnowDbContext dbContext,
    Services.Quota.IQuotaService quotaService) : IRequestHandler<UpdateOrganizationSubscriptionCommand, UpdateOrganizationSubscriptionResponse>
{
    public async Task<UpdateOrganizationSubscriptionResponse> Handle(UpdateOrganizationSubscriptionCommand request, CancellationToken cancellationToken)
    {
        // Must ignore global query filters to see the organization regardless of tenant
        var organization = await dbContext.Organizations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(o => o.Id == request.Id, cancellationToken);

        if (organization == null)
        {
            throw new InvalidOperationException("Organization not found.");
        }

        // Update fields
        var newPlan = SubscriptionPlan.FromName(request.SubscriptionTier);
        organization.ChangePlan(newPlan);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Resolve any locked/overage discrepancies if the tier changed
        await quotaService.ResolveQuotaDiscrepanciesAsync(organization.Id, cancellationToken);

        return new UpdateOrganizationSubscriptionResponse
        {
            Id = organization.Id,
            Name = organization.Name,
            SubscriptionTier = organization.Plan.Name,
            StripeCustomerId = organization.BillingIdentity?.CustomerId,
            IsPaidTier = organization.Plan.TierLevel > 0,
            UpdatedAt = DateTime.UtcNow
        };
    }
}
