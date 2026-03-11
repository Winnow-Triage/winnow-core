using Winnow.Server.Domain.Core;

namespace Winnow.Server.Domain.Organizations.Events;

public sealed record OrganizationInvitationAcceptedEvent(Guid InvitationId, Guid OrganizationId, string Email) : IDomainEvent;
