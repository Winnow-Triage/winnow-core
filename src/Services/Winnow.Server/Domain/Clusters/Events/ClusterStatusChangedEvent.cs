using Winnow.Server.Domain.Clusters.ValueObjects;
using Winnow.Server.Domain.Core;

namespace Winnow.Server.Domain.Clusters.Events;

public sealed record ClusterStatusChangedEvent(Guid ClusterId, ClusterStatus OldStatus, ClusterStatus NewStatus) : IDomainEvent;
