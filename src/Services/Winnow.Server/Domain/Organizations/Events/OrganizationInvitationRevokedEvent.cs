using Winnow.Server.Domain.Core;

namespace Winnow.Server.Domain.Organizations.Events;

public sealed record OrganizationInvitationRevokedEvent(Guid InvitationId, Guid OrganizationId) : IDomainEvent;
