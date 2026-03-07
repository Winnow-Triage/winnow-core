using Winnow.Server.Domain.Core;

namespace Winnow.Server.Domain.Clusters.Events;

public sealed record ClusterReportAddedEvent(Guid ClusterId, Guid ReportId) : IDomainEvent;
