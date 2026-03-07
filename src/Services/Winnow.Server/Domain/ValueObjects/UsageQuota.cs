namespace Winnow.Server.Domain.ValueObjects;

public readonly record struct UsageQuota
{
    public int Limit { get; init; }
    public int Consumed { get; init; }

    public UsageQuota(int limit, int consumed = 0)
    {
        if (limit < 0) throw new ArgumentException("Limit cannot be negative.", nameof(limit));
        if (consumed < 0) throw new ArgumentException("Consumed cannot be negative.", nameof(consumed));
        if (consumed > limit) throw new ArgumentException("Consumed cannot be greater than limit.", nameof(consumed));

        Limit = limit;
        Consumed = consumed;
    }

    public bool CanConsume() => Consumed < Limit;

    public UsageQuota Consume()
    {
        if (!CanConsume()) throw new InvalidOperationException("Usage limit has been reached.");
        return this with { Consumed = Consumed + 1 };
    }

    public UsageQuota WithLimit(int newLimit) => this with { Limit = newLimit };
    public UsageQuota Reset() => this with { Consumed = 0 };
}
