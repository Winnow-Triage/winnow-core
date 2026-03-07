using Winnow.Server.Domain.ValueObjects;

namespace Winnow.Server.Domain.Events;

public record AiSummaryLimitReachedEvent(Guid OrganizationId, SubscriptionPlan Plan) : IDomainEvent;
