using Winnow.API.Domain.Core;

namespace Winnow.API.Domain.Teams.Events;

public sealed record TeamRenamedEvent(Guid TeamId, string OldName, string NewName) : IDomainEvent;
