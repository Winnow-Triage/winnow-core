using Winnow.API.Domain.Clusters.ValueObjects;
using Winnow.API.Domain.Core;

namespace Winnow.API.Domain.Clusters.Events;

public sealed record ClusterStatusChangedEvent(Guid ClusterId, ClusterStatus OldStatus, ClusterStatus NewStatus) : IDomainEvent;
