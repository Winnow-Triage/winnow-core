using Winnow.API.Domain.Organizations.ValueObjects;

namespace Winnow.API.Infrastructure.Billing;

public interface IStripePlanMapper
{
    SubscriptionPlan MapToDomainPlan(Stripe.Subscription subscription);
}