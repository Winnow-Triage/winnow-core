using Winnow.Server.Domain.Core;

namespace Winnow.Server.Domain.Clusters.Events;

public sealed record ClusterSummarizedEvent(Guid ClusterId, Guid OrganizationId, int CriticalityScore) : IDomainEvent;
