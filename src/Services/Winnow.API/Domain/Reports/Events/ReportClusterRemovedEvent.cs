using Winnow.API.Domain.Core;

namespace Winnow.API.Domain.Reports.Events;

public sealed record ReportClusterRemovedEvent(Guid ReportId, Guid PreviousClusterId) : IDomainEvent;
