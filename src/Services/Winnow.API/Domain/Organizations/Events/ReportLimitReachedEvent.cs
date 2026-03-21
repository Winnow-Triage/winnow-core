using Winnow.API.Domain.Core;
using Winnow.API.Domain.Organizations.ValueObjects;

namespace Winnow.API.Domain.Organizations.Events;

public sealed record ReportLimitReachedEvent(Guid OrganizationId, SubscriptionPlan Plan) : IDomainEvent;
