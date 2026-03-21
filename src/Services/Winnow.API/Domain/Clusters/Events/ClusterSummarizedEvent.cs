using Winnow.API.Domain.Core;

namespace Winnow.API.Domain.Clusters.Events;

public sealed record ClusterSummarizedEvent(Guid ClusterId, Guid OrganizationId, int CriticalityScore) : IDomainEvent;
