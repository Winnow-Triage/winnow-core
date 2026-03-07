namespace Winnow.Server.Domain.ValueObjects;

public readonly record struct SubscriptionPlan
{
    public string Name { get; init; }
    public int TierLevel { get; init; }
    public int MonthlyReportLimit { get; init; }
    public int MonthlySummaryLimit { get; init; }

    public SubscriptionPlan(string name, int tierLevel, int monthlyReportLimit, int monthlySummaryLimit)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Subscription plan name is required.", nameof(name));

        if (tierLevel < 0)
            throw new ArgumentException("Subscription plan tier level cannot be negative.", nameof(tierLevel));

        if (monthlyReportLimit < 0)
            throw new ArgumentException("Monthly report limit cannot be negative.", nameof(monthlyReportLimit));

        if (monthlySummaryLimit < 0)
            throw new ArgumentException("Monthly summary limit cannot be negative.", nameof(monthlySummaryLimit));

        Name = name;
        TierLevel = tierLevel;
        MonthlyReportLimit = monthlyReportLimit;
        MonthlySummaryLimit = monthlySummaryLimit;
    }

    public bool IsUpgradeFrom(SubscriptionPlan currentPlan)
    {
        return this.TierLevel > currentPlan.TierLevel;
    }

    public static readonly SubscriptionPlan Free = new("Free", 0, 0, 0);
    public static readonly SubscriptionPlan Starter = new("Starter", 1, 10, 5);
    public static readonly SubscriptionPlan Pro = new("Pro", 2, 50, 25);
    public static readonly SubscriptionPlan Enterprise = new("Enterprise", 3, int.MaxValue, int.MaxValue);
}