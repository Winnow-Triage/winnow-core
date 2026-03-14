using FastEndpoints;
using FluentValidation;
using Winnow.Server.Domain.Organizations.ValueObjects;

namespace Winnow.Server.Features.Admin.Organizations.UpdateSubscription;

public class UpdateOrganizationSubscriptionRequestValidator : Validator<UpdateOrganizationSubscriptionRequest>
{
    public UpdateOrganizationSubscriptionRequestValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Organization ID is required.");

        RuleFor(x => x.SubscriptionTier)
            .NotEmpty()
            .WithMessage("Subscription tier is required.")
            .Must(BeAValidPlan)
            .WithMessage(x =>
            {
                var allowed = string.Join(", ", SubscriptionPlan.List().Select(p => p.Name));
                return $"Invalid subscription tier '{x.SubscriptionTier}'. Allowed values: {allowed}";
            });
    }

    private bool BeAValidPlan(string tierName)
    {
        return SubscriptionPlan.TryFromName(tierName, out _);
    }
}