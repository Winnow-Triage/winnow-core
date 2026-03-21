namespace Winnow.API.Domain.Organizations.ValueObjects;

public readonly record struct BillingIdentity
{
    public string Provider { get; init; } // e.g., "Stripe", "Paddle"
    public string CustomerId { get; init; }
    public string? SubscriptionId { get; init; } // Nullable because a Free tier user might have a CustomerId but no active Subscription

    public BillingIdentity(string provider, string customerId, string? subscriptionId = null)
    {
        if (string.IsNullOrWhiteSpace(provider))
            throw new ArgumentException("Billing provider is required.", nameof(provider));

        if (string.IsNullOrWhiteSpace(customerId))
            throw new ArgumentException("Customer ID is required.", nameof(customerId));

        Provider = provider;
        CustomerId = customerId;
        SubscriptionId = subscriptionId;
    }

    public bool HasActiveSubscription => !string.IsNullOrWhiteSpace(SubscriptionId);
}