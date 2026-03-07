using Winnow.Server.Domain.Core;

namespace Winnow.Server.Domain.Teams.Events;

public sealed record TeamRenamedEvent(Guid TeamId, string OldName, string NewName) : IDomainEvent;
