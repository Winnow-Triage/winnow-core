namespace Winnow.Server.Domain.Events;

public record ReportRemovedFromClusterEvent(Guid ReportId, Guid PreviousClusterId) : IDomainEvent;
