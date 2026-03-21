namespace Winnow.Contracts;

public record GenerateClusterSummaryEvent(Guid ClusterId, Guid OrganizationId, Guid ProjectId);

public record ReportCreatedEvent
{
    public Guid ReportId { get; init; }
    public Guid CurrentOrganizationId { get; init; }
    public Guid ProjectId { get; init; }
    public string Title { get; init; } = default!;
    public string Message { get; init; } = default!;
    public string? StackTrace { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? Metadata { get; init; }
}

public record ReportSanitizedEvent
{
    public Guid ReportId { get; init; }
    public Guid CurrentOrganizationId { get; init; }
    public Guid ProjectId { get; init; }
    public string Title { get; init; } = default!;
    public string Message { get; init; } = default!;
    public string? StackTrace { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? Metadata { get; init; }
}
