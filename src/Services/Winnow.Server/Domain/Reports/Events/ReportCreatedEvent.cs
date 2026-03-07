using Winnow.Server.Domain.Core;

namespace Winnow.Server.Domain.Reports.Events;

public sealed record ReportCreatedEvent(
    Guid ReportId,
    Guid OrganizationId,
    Guid ProjectId,
    string Title) : IDomainEvent;