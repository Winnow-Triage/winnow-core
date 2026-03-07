namespace Winnow.Server.Domain.Organizations.ValueObjects;

public readonly record struct UsageQuota
{
    public int Limit { get; init; }
    public int GraceLimit { get; init; }
    public int Consumed { get; init; }

    public UsageQuota(int limit, int graceLimit, int consumed = 0)
    {
        if (limit < 0) throw new ArgumentException("Limit cannot be negative.", nameof(limit));
        if (graceLimit < limit) throw new ArgumentException("Grace limit cannot be less than the base limit.", nameof(graceLimit));
        if (consumed < 0) throw new ArgumentException("Consumed cannot be negative.", nameof(consumed));

        Limit = limit;
        GraceLimit = graceLimit;
        Consumed = consumed;
    }

    public bool IsOverage() => Consumed >= Limit;
    public bool IsGraceExhausted() => Consumed >= GraceLimit;

    public UsageQuota Consume() => this with { Consumed = Consumed + 1 };

    public UsageQuota WithLimits(int newLimit, int newGraceLimit) => this with { Limit = newLimit, GraceLimit = newGraceLimit };

    public UsageQuota Reset() => this with { Consumed = 0 };
}