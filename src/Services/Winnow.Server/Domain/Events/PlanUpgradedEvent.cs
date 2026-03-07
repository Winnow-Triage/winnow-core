using Winnow.Server.Domain.ValueObjects;

namespace Winnow.Server.Domain.Events;

public record PlanUpgradedEvent(
    Guid OrganizationId,
    SubscriptionPlan OldPlan,
    SubscriptionPlan NewPlan
) : IDomainEvent;