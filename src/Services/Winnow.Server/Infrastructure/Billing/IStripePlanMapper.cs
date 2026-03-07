using Winnow.Server.Domain.Organizations.ValueObjects;

namespace Winnow.Server.Infrastructure.Billing;

public interface IStripePlanMapper
{
    SubscriptionPlan MapToDomainPlan(Stripe.Subscription subscription);
}