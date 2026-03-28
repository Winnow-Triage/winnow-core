using Winnow.API.Domain.Core;

namespace Winnow.API.Domain.Organizations.Events;

public sealed record OrganizationInvitationAcceptedEvent(Guid InvitationId, Guid OrganizationId, string Email) : IDomainEvent;
