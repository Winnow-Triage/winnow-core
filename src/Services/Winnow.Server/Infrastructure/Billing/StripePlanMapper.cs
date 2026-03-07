using Winnow.Server.Domain.Organizations.ValueObjects;

namespace Winnow.Server.Infrastructure.Billing;

public class StripePlanMapper(IConfiguration config) : IStripePlanMapper
{
    public SubscriptionPlan MapToDomainPlan(Stripe.Subscription subscription)
    {
        if (subscription == null || (subscription.Status != "active" && subscription.Status != "trialing"))
        {
            return SubscriptionPlan.Free; // Ensure this is your domain's default Plan
        }

        var priceId = subscription.Items?.Data?.FirstOrDefault()?.Price?.Id;

        return priceId switch
        {
            var id when id == config["Stripe:Prices:Starter"] => SubscriptionPlan.Starter,
            var id when id == config["Stripe:Prices:Pro"] => SubscriptionPlan.Pro,
            var id when id == config["Stripe:Prices:Enterprise"] => SubscriptionPlan.Enterprise,
            _ => SubscriptionPlan.Free
        };
    }
}