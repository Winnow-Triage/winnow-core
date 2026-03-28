using Winnow.API.Domain.Core;

namespace Winnow.API.Domain.Organizations.Events;

public sealed record BillingIdentityLinkedEvent(Guid OrganizationId, string BillingProvider) : IDomainEvent;