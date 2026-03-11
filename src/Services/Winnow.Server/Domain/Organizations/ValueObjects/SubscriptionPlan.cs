namespace Winnow.Server.Domain.Organizations.ValueObjects;

/// <summary>
/// Represents a subscription plan for an organization.
/// </summary>
public readonly record struct SubscriptionPlan
{
    public string Name { get; init; }
    public int TierLevel { get; init; }
    public int MonthlyReportLimit { get; init; }
    public int MonthlyReportGraceLimit { get; init; }
    public int MonthlySummaryLimit { get; init; }
    public int MonthlySummaryGraceLimit { get; init; }

    /// <summary>
    /// Creates a new instance of the <see cref="SubscriptionPlan"/> class.
    /// </summary>
    /// <param name="name">The name of the subscription plan.</param>
    /// <param name="tierLevel">The tier level of the subscription plan.</param>
    /// <param name="monthlyReportLimit">The monthly report limit of the subscription plan.</param>
    /// <param name="monthlyReportGraceLimit">The monthly report grace limit of the subscription plan</param>
    /// <param name="monthlySummaryLimit">The monthly summary limit of the subscription plan.</param>
    /// <param name="monthlySummaryGraceLimit">The monthly summary grace limit of the subscription plan</param>
    public SubscriptionPlan(string name, int tierLevel, int monthlyReportLimit, int monthlyReportGraceLimit, int monthlySummaryLimit, int monthlySummaryGraceLimit)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Subscription plan name is required.", nameof(name));

        if (tierLevel < 0)
            throw new ArgumentException("Subscription plan tier level cannot be negative.", nameof(tierLevel));

        if (monthlyReportLimit < 0)
            throw new ArgumentException("Monthly report limit cannot be negative.", nameof(monthlyReportLimit));

        if (monthlyReportGraceLimit < 0)
            throw new ArgumentException("Monthly report grace limit cannot be negative.", nameof(monthlyReportGraceLimit));

        if (monthlySummaryLimit < 0)
            throw new ArgumentException("Monthly summary limit cannot be negative.", nameof(monthlySummaryLimit));

        if (monthlySummaryGraceLimit < 0)
            throw new ArgumentException("Monthly summary grace limit cannot be negative.", nameof(monthlySummaryGraceLimit));

        Name = name;
        TierLevel = tierLevel;
        MonthlyReportLimit = monthlyReportLimit;
        MonthlyReportGraceLimit = monthlyReportGraceLimit;
        MonthlySummaryLimit = monthlySummaryLimit;
        MonthlySummaryGraceLimit = monthlySummaryGraceLimit;
    }

    /// <summary>
    /// Checks if the current plan is an upgrade from the specified plan.
    /// </summary>
    /// <param name="currentPlan">The current plan to compare against.</param>
    /// <returns>True if the current plan is an upgrade from the specified plan, false otherwise.</returns>
    public bool IsUpgradeFrom(SubscriptionPlan currentPlan)
    {
        return TierLevel > currentPlan.TierLevel;
    }

    /// <summary>
    /// Checks if the current plan is at least the specified plan.
    /// </summary>
    /// <param name="requiredPlan">The required plan to compare against.</param>
    /// <returns>True if the current plan is at least the specified plan, false otherwise.</returns>
    public bool IsAtLeast(SubscriptionPlan requiredPlan)
    {
        return TierLevel >= requiredPlan.TierLevel;
    }

    /// <summary>
    /// Attempts to get a subscription plan from a name without throwing an exception.
    /// </summary>
    /// <param name="name">The name of the subscription plan.</param>
    /// <param name="plan">The resolved plan if successful; otherwise null.</param>
    /// <returns>True if the plan was found, false otherwise.</returns>
    public static bool TryFromName(string? name, out SubscriptionPlan? plan)
    {
        plan = name?.ToLowerInvariant() switch
        {
            "free" => Free,
            "starter" => Starter,
            "pro" => Pro,
            "enterprise" => Enterprise,
            _ => null
        };

        return plan != null;
    }

    public static SubscriptionPlan FromName(string name)
    {
        if (TryFromName(name, out var plan))
        {
            return plan!.Value;
        }

        throw new ArgumentException($"Unknown or unsupported plan name: {name}");
    }

    public static List<SubscriptionPlan> List()
    {
        return [Free, Starter, Pro, Enterprise];
    }

    /// <summary>
    /// Gets the free subscription plan.
    /// </summary>
    public static readonly SubscriptionPlan Free = new("Free", 0, 50, 100, 0, 0);

    /// <summary>
    /// Gets the starter subscription plan.
    /// </summary>
    public static readonly SubscriptionPlan Starter = new("Starter", 1, 500, 1000, 50, 50);

    /// <summary>
    /// Gets the pro subscription plan.
    /// </summary>
    public static readonly SubscriptionPlan Pro = new("Pro", 2, int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue);

    /// <summary>
    /// Gets the enterprise subscription plan.
    /// </summary>
    public static readonly SubscriptionPlan Enterprise = new("Enterprise", 3, int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue);
}