using Winnow.API.Domain.Core;

namespace Winnow.API.Domain.Reports.Events;

public sealed record ReportCreatedEvent(
    Guid ReportId,
    Guid OrganizationId,
    Guid ProjectId,
    string Title) : IDomainEvent;