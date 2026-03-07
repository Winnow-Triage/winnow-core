using Winnow.Server.Domain.ValueObjects;

namespace Winnow.Server.Domain.Events;

public record ReportLimitReachedEvent(Guid OrganizationId, SubscriptionPlan Plan) : IDomainEvent;
