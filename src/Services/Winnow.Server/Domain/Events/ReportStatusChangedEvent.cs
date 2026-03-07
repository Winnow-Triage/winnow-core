using Winnow.Server.Domain.ValueObjects;

namespace Winnow.Server.Domain.Events;

public record ReportStatusChangedEvent(Guid ReportId, ReportStatus OldStatus, ReportStatus NewStatus) : IDomainEvent;
