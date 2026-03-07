using Winnow.Server.Domain.Core;
using Winnow.Server.Domain.Organizations.ValueObjects;

namespace Winnow.Server.Domain.Organizations.Events;

public sealed record OrganizationPlanDowngradedEvent(
    Guid OrganizationId,
    SubscriptionPlan OldPlan,
    SubscriptionPlan NewPlan
) : IDomainEvent;