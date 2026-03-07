using Winnow.Server.Domain.Core;

namespace Winnow.Server.Domain.Organizations.Events;

public sealed record BillingIdentityLinkedEvent(Guid OrganizationId, string BillingProvider) : IDomainEvent;