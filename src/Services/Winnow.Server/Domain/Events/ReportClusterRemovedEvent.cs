namespace Winnow.Server.Domain.Events;

public record ReportClusterRemovedEvent(Guid ReportId, Guid PreviousClusterId) : IDomainEvent;
