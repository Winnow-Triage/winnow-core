using Winnow.API.Domain.Common;
using Winnow.API.Domain.Organizations;
using Winnow.API.Domain.Organizations.Events;
using Winnow.API.Domain.Organizations.ValueObjects;

namespace Winnow.API.Tests;

public class OrganizationDomainEventTests
{
    // ──────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────

    private static Organization CreateStarterOrg() =>
        new("Acme Corp", new Email("contact@acme.com"), SubscriptionPlan.Starter);

    private static Organization CreateFreeOrg() =>
        new("Acme Corp", new Email("contact@acme.com"), SubscriptionPlan.Free);

    // ──────────────────────────────────────────────────────────────
    // Plan events
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void ChangePlan_ToHigherTier_RaisesPlanUpgradedEvent()
    {
        var org = CreateStarterOrg();

        org.ChangePlan(SubscriptionPlan.Pro);

        var evt = Assert.Single(org.DomainEvents.OfType<OrganizationPlanUpgradedEvent>());
        Assert.Equal(org.Id, evt.OrganizationId);
        Assert.Equal(SubscriptionPlan.Starter, evt.OldPlan);
        Assert.Equal(SubscriptionPlan.Pro, evt.NewPlan);
    }

    [Fact]
    public void ChangePlan_ToLowerTier_RaisesPlanDowngradedEvent()
    {
        var org = CreateStarterOrg();

        org.ChangePlan(SubscriptionPlan.Free);

        var evt = Assert.Single(org.DomainEvents.OfType<OrganizationPlanDowngradedEvent>());
        Assert.Equal(org.Id, evt.OrganizationId);
        Assert.Equal(SubscriptionPlan.Starter, evt.OldPlan);
        Assert.Equal(SubscriptionPlan.Free, evt.NewPlan);
    }

    [Fact]
    public void ChangePlan_ToHigherTier_DoesNotRaisePlanDowngradedEvent()
    {
        var org = CreateStarterOrg();

        org.ChangePlan(SubscriptionPlan.Pro);

        Assert.Empty(org.DomainEvents.OfType<OrganizationPlanDowngradedEvent>());
    }

    // ──────────────────────────────────────────────────────────────
    // Quota limit events
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void RecordReportUsage_WhenLimitReached_RaisesReportLimitReachedEvent()
    {
        // Free plan has a limit of 50 — using a custom plan with 0 limit to hit it immediately
        var zeroLimitPlan = new SubscriptionPlan("Zero", 0, 0, 0, 0, 0);
        var org = new Organization("Test Org", new Email("test@example.com"), zeroLimitPlan);
        org.ClearDomainEvents();

        org.RecordReportUsage();

        var evt = Assert.Single(org.DomainEvents.OfType<ReportLimitReachedEvent>());
        Assert.Equal(org.Id, evt.OrganizationId);
        Assert.Equal(org.Plan, evt.Plan);
    }

    [Fact]
    public void RecordAiSummaryUsage_WhenLimitReached_RaisesAiSummaryLimitReachedEvent()
    {
        var org = CreateFreeOrg();
        org.ClearDomainEvents();

        org.RecordAiSummaryUsage();

        var evt = Assert.Single(org.DomainEvents.OfType<AiSummaryLimitReachedEvent>());
        Assert.Equal(org.Id, evt.OrganizationId);
        Assert.Equal(SubscriptionPlan.Free, evt.Plan);
    }

    [Fact]
    public void RecordReportUsage_WhenUnderLimit_DoesNotRaiseEvent()
    {
        var org = CreateStarterOrg(); // 10 report limit

        org.RecordReportUsage();

        Assert.Empty(org.DomainEvents.OfType<ReportLimitReachedEvent>());
    }

    // ──────────────────────────────────────────────────────────────
    // ClearDomainEvents
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void ClearDomainEvents_RemovesAllRaisedEvents()
    {
        var org = CreateStarterOrg();
        org.ChangePlan(SubscriptionPlan.Pro);
        Assert.NotEmpty(org.DomainEvents);

        org.ClearDomainEvents();

        Assert.Empty(org.DomainEvents);
    }

    [Fact]
    public void MultipleOperations_AccumulateEventsInOrder()
    {
        var org = CreateStarterOrg();
        org.ClearDomainEvents();

        org.ChangePlan(SubscriptionPlan.Pro);
        org.ChangePlan(SubscriptionPlan.Starter);

        Assert.Equal(2, org.DomainEvents.Count);
        Assert.IsType<OrganizationPlanUpgradedEvent>(org.DomainEvents[0]);
        Assert.IsType<OrganizationPlanDowngradedEvent>(org.DomainEvents[1]);
    }
}
