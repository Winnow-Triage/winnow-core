using Winnow.API.Domain.Common;
using Winnow.API.Domain.Core;
using Winnow.API.Domain.Organizations.Events;
using Winnow.API.Domain.Organizations.ValueObjects;
using Winnow.API.Domain.Projects;
using Winnow.API.Domain.Teams;

namespace Winnow.API.Domain.Organizations;

/// <summary>
/// Represents an organization that uses the Winnow service.
/// </summary>
public class Organization : IAggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    public void ClearDomainEvents() => _domainEvents.Clear();

    public Guid Id { get; private set; }

    private readonly List<Guid> _teamIds = [];
    public IReadOnlyCollection<Guid> Teams => _teamIds.AsReadOnly();

    private readonly List<Guid> _projectIds = [];
    public IReadOnlyCollection<Guid> Projects => _projectIds.AsReadOnly();

    private readonly List<Guid> _memberIds = [];
    public IReadOnlyCollection<Guid> Members => _memberIds.AsReadOnly();

    // EF Core Navigation Properties - Private/Internal to maintain DDD boundaries
    private readonly List<Team> _teams = [];
    private readonly List<Project> _projects = [];
    private readonly List<OrganizationMember> _memberships = [];

    internal IReadOnlyCollection<Team> OrganizationTeams => _teams.AsReadOnly();
    internal IReadOnlyCollection<Project> OrganizationProjects => _projects.AsReadOnly();
    internal IReadOnlyCollection<OrganizationMember> OrganizationMemberships => _memberships.AsReadOnly();

    public string Name { get; private set; }
    public Email ContactEmail { get; private set; }
    public SubscriptionPlan Plan { get; private set; }
    public BillingIdentity? BillingIdentity { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public bool IsSuspended { get; private set; }
    public bool IsLocked { get; private set; }
    public bool HasReachedMonthlyLimit { get; private set; }

    public UsageQuota ReportQuota { get; private set; }
    public UsageQuota SummaryQuota { get; private set; }

    public OrganizationSettings Settings { get; private set; } = null!;

    // <summary>
    // Private parameterless constructor specifically for Entity Framework Core.
    // The null! tells the compiler to ignore the nullability warning here.
    // </summary>
    private Organization()
    {
        Name = null!;
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
        CreatedAt = DateTime.UtcNow;
        ReportQuota = new UsageQuota(Plan.MonthlyReportLimit, Plan.MonthlyReportGraceLimit);
        SummaryQuota = new UsageQuota(Plan.MonthlySummaryLimit, Plan.MonthlySummaryGraceLimit);
        Settings = OrganizationSettings.Create(Id);

        _domainEvents.Add(new OrganizationCreatedEvent(Id, Name, ContactEmail.Value));
    }

    public void Rename(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("Organization name is required.", nameof(newName));

        var sanitizedName = newName.Trim();

        // Don't do anything if the name didn't actually change!
        if (Name == sanitizedName)
            return;

        Name = sanitizedName;

        _domainEvents.Add(new OrganizationRenamedEvent(Id, Name));
    }

    public void ChangeContactEmail(Email newEmail)
    {
        if (ContactEmail == newEmail)
            return;

        ContactEmail = newEmail;

        // Highly recommended! You might need an event handler to sync this to Stripe
        _domainEvents.Add(new OrganizationContactEmailChangedEvent(Id, ContactEmail.Value));
    }

    /// <summary>
    /// Checks if the organization can generate an AI summary.
    /// </summary>
    public bool CanGenerateAiSummary()
    {
        return !SummaryQuota.IsGraceExhausted();
    }

    /// <summary>
    /// Checks if the organization can generate a report.
    /// </summary>
    public bool CanGenerateReport()
    {
        return !ReportQuota.IsGraceExhausted();
    }


    /// <summary>
    /// Records the usage of an AI summary.
    /// </summary>
    public void RecordAiSummaryUsage()
    {
        if (IsLocked) return; // Completely frozen

        SummaryQuota = SummaryQuota.Consume();

        if (SummaryQuota.IsOverage())
        {
            if (!HasReachedMonthlyLimit)
            {
                HasReachedMonthlyLimit = true;
                _domainEvents.Add(new AiSummaryLimitReachedEvent(Id, Plan));
            }
        }
    }
    /// <summary>
    /// Records the usage of a report.
    /// </summary>
    public void RecordReportUsage()
    {
        if (IsLocked) return; // Completely frozen

        ReportQuota = ReportQuota.Consume();

        if (ReportQuota.IsOverage())
        {
            if (!HasReachedMonthlyLimit)
            {
                HasReachedMonthlyLimit = true;
                _domainEvents.Add(new ReportLimitReachedEvent(Id, Plan));
            }

            if (ReportQuota.IsGraceExhausted())
            {
                IsLocked = true;
                _domainEvents.Add(new OrganizationLockedEvent(Id));
            }
        }
    }

    // <summary>
    // Changes the organization's plan.
    // </summary>
    // <param name="newPlan">The new plan to change to.</param>
    public void ChangePlan(SubscriptionPlan newPlan)
    {
        bool isUpgrade = newPlan.IsUpgradeFrom(Plan);
        var oldPlan = Plan;

        Plan = newPlan;

        if (isUpgrade)
        {
            ReportQuota = ReportQuota.WithLimits(newPlan.MonthlyReportLimit, newPlan.MonthlyReportGraceLimit);
            SummaryQuota = SummaryQuota.WithLimits(newPlan.MonthlySummaryLimit, newPlan.MonthlySummaryGraceLimit);
            _domainEvents.Add(new OrganizationPlanUpgradedEvent(Id, oldPlan, newPlan));
        }
        else
        {
            _domainEvents.Add(new OrganizationPlanDowngradedEvent(Id, oldPlan, newPlan));
        }
    }


    public void CancelSubscription()
    {
        if (BillingIdentity == null || BillingIdentity.Value.SubscriptionId == null)
            return; // Nothing to cancel

        // Keep the CustomerId, but wipe the SubscriptionId
        BillingIdentity = new BillingIdentity(
            BillingIdentity.Value.Provider,
            BillingIdentity.Value.CustomerId,
            null);

        // You already have the perfect event for this!
        ChangePlan(SubscriptionPlan.Free);
    }

    // <summary>
    // Resets the organization's monthly usage.
    // </summary>
    public void ResetMonthlyUsage()
    {
        ReportQuota = ReportQuota.Reset();
        SummaryQuota = SummaryQuota.Reset();
        HasReachedMonthlyLimit = false;
    }

    public void Suspend(string Reasoning)
    {
        IsSuspended = true;
        _domainEvents.Add(new OrganizationSuspendedEvent(Id, Reasoning));
    }

    public void Activate()
    {
        IsSuspended = false;
        _domainEvents.Add(new OrganizationActivatedEvent(Id));
    }

    public void LinkBillingIdentity(BillingIdentity identity)
    {
        BillingIdentity = identity;
        _domainEvents.Add(new BillingIdentityLinkedEvent(Id, identity.Provider));
    }

    public bool HasActiveSubscription => BillingIdentity?.HasActiveSubscription ?? false;
}