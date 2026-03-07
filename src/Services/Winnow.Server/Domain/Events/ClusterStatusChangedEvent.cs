using Winnow.Server.Domain.ValueObjects;

namespace Winnow.Server.Domain.Events;

public record ClusterStatusChangedEvent(Guid ClusterId, ClusterStatus OldStatus, ClusterStatus NewStatus) : IDomainEvent;
