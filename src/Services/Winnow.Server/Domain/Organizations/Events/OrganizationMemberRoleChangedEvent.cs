using Winnow.Server.Domain.Core;

namespace Winnow.Server.Domain.Organizations.Events;

public sealed record OrganizationMemberRoleChangedEvent(Guid MemberId, Guid OrganizationId, string OldRole, string NewRole) : IDomainEvent;
