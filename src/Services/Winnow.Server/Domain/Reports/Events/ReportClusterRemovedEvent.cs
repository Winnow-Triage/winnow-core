using Winnow.Server.Domain.Core;

namespace Winnow.Server.Domain.Reports.Events;

public sealed record ReportClusterRemovedEvent(Guid ReportId, Guid PreviousClusterId) : IDomainEvent;
