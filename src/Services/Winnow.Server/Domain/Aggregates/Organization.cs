using Winnow.Server.Domain;
using Winnow.Server.Domain.Events;
using Winnow.Server.Domain.ValueObjects;

namespace Winnow.Server.Domain.Aggregates;

/// <summary>
/// Represents an organization that uses the Winnow service.
/// </summary>
public class Organization : IAggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    public void ClearDomainEvents() => _domainEvents.Clear();

    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public Email ContactEmail { get; private set; }
    public SubscriptionPlan Plan { get; private set; }

    public UsageQuota ReportQuota { get; private set; }
    public UsageQuota SummaryQuota { get; private set; }

    // <summary>
    // Private parameterless constructor specifically for Entity Framework Core.
    // The null! tells the compiler to ignore the nullability warning here.
    // </summary>
    private Organization()
    {
        Name = null!;
        // Plan, UsageQuota, and ContactEmail are structs — they're value types and can't be null,
        // so EF Core default-initializes them to their zero-value automatically.
    }

    // <summary>
    // Public constructor for creating a brand new Organization
    // </summary>
    // <param name="name">The name of the organization.</param>
    // <param name="contactEmail">The contact email of the organization.</param>
    // <param name="plan">The subscription plan of the organization.</param>
    public Organization(string name, Email contactEmail, SubscriptionPlan? plan = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Organization name is required.", nameof(name));

        Id = Guid.NewGuid();
        Name = name;
        ContactEmail = contactEmail;
        Plan = plan ?? SubscriptionPlan.Free;
        ReportQuota = new UsageQuota(Plan.MonthlyReportLimit);
        SummaryQuota = new UsageQuota(Plan.MonthlySummaryLimit);
    }

    // <summary>
    // Checks if the organization can generate an AI summary.
    // </summary>
    public bool CanGenerateAiSummary()
    {
        return SummaryQuota.CanConsume();
    }

    // <summary>
    // Records the usage of an AI summary.
    // </summary>
    public void RecordAiSummaryUsage()
    {
        if (!CanGenerateAiSummary())
        {
            _domainEvents.Add(new AiSummaryLimitReachedEvent(Id, Plan));
            throw new InvalidOperationException(
                $"Organization {Name} on the {Plan.Name} plan has reached its AI summary limit of {Plan.MonthlySummaryLimit}."
            );
        }

        SummaryQuota = SummaryQuota.Consume();
    }

    // <summary>
    // Checks if the organization can generate a report.
    // </summary>
    public bool CanGenerateReport()
    {
        return ReportQuota.CanConsume();
    }

    // <summary>
    // Records the usage of a report.
    // </summary>
    public void RecordReportUsage()
    {
        if (!CanGenerateReport())
        {
            _domainEvents.Add(new ReportLimitReachedEvent(Id, Plan));
            throw new InvalidOperationException(
                $"Organization {Name} on the {Plan.Name} plan has reached its report limit of {Plan.MonthlyReportLimit}."
            );
        }

        ReportQuota = ReportQuota.Consume();
    }

    // <summary>
    // Changes the organization's plan.
    // </summary>
    // <param name="newPlan">The new plan to change to.</param>
    public void ChangePlan(SubscriptionPlan newPlan)
    {
        // The Aggregate asks the Value Object to do the comparison!
        bool isUpgrade = newPlan.IsUpgradeFrom(Plan);
        var oldPlan = Plan;

        Plan = newPlan;

        if (isUpgrade)
        {
            ReportQuota = ReportQuota.WithLimit(newPlan.MonthlyReportLimit);
            SummaryQuota = SummaryQuota.WithLimit(newPlan.MonthlySummaryLimit);
            _domainEvents.Add(new PlanUpgradedEvent(Id, oldPlan, newPlan));
        }
        else
        {
            _domainEvents.Add(new PlanDowngradedEvent(Id, oldPlan, newPlan));
        }
    }

    // <summary>
    // Resets the organization's monthly usage.
    // </summary>
    public void ResetMonthlyUsage()
    {
        ReportQuota = new UsageQuota(Plan.MonthlyReportLimit);
        SummaryQuota = new UsageQuota(Plan.MonthlySummaryLimit);
    }
}