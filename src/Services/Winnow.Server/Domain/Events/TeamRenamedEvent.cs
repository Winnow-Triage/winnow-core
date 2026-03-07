namespace Winnow.Server.Domain.Events;

public record TeamRenamedEvent(Guid TeamId, string OldName, string NewName) : IDomainEvent;
