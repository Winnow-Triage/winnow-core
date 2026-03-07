using Winnow.Server.Domain.Events;
using Winnow.Server.Domain.ValueObjects;

namespace Winnow.Server.Domain.Aggregates;

/// <summary>
/// Represents a bug report submitted to Winnow.
/// A Report can exist without a Cluster (floating free), or be assigned to one.
/// </summary>
public class Report : IAggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    public void ClearDomainEvents() => _domainEvents.Clear();

    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid OrganizationId { get; private set; }
    public string Title { get; private set; }
    public string Message { get; private set; }
    public string? StackTrace { get; private set; }
    public string? StackTraceHash { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public ReportStatus Status { get; private set; }

    // Cluster membership — null means the report is unassigned
    public Guid? ClusterId { get; private set; }

    // Clustering suggestion from the AI
    public Guid? SuggestedClusterId { get; private set; }
    public ConfidenceScore? SuggestedConfidenceScore { get; private set; }

    // Similarity embedding and confidence from when the report was matched
    public ConfidenceScore? ConfidenceScore { get; private set; }
    public float[]? Embedding { get; private set; }

    // Overage / billing state
    public bool IsOverage { get; private set; }
    public bool IsLocked { get; private set; }

    public string? AssignedTo { get; private set; }
    public Uri? ExternalUrl { get; private set; }

    // Private EF constructor
    private Report()
    {
        Title = null!;
        Message = null!;
    }

    public Report(
        Guid projectId,
        Guid organizationId,
        string title,
        string message,
        string? stackTrace = null,
        string? stackTraceHash = null,
        float[]? embedding = null,
        Uri? externalUrl = null)
    {
        if (projectId == Guid.Empty)
            throw new ArgumentException("Project ID is required.", nameof(projectId));
        if (organizationId == Guid.Empty)
            throw new ArgumentException("Organization ID is required.", nameof(organizationId));
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Report title is required.", nameof(title));
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Report message is required.", nameof(message));

        Id = Guid.NewGuid();
        ProjectId = projectId;
        OrganizationId = organizationId;
        Title = title;
        Message = message;
        StackTrace = stackTrace;
        StackTraceHash = stackTraceHash;
        Embedding = embedding;
        ExternalUrl = externalUrl;
        Status = ReportStatus.New;
        CreatedAt = DateTime.UtcNow;
    }

    // ──────────────────────────────────────────────────────────────
    // Cluster assignment
    // ──────────────────────────────────────────────────────────────

    public void AssignToCluster(Guid clusterId, ConfidenceScore? confidenceScore = null)
    {
        if (clusterId == Guid.Empty)
            throw new ArgumentException("Cluster ID must not be empty.", nameof(clusterId));

        ClusterId = clusterId;
        ConfidenceScore = confidenceScore;
        _domainEvents.Add(new ReportClusterAssignedEvent(Id, clusterId));
    }

    public void RemoveFromCluster()
    {
        if (ClusterId is null) return;

        var previous = ClusterId.Value;
        ClusterId = null;
        ConfidenceScore = null;
        _domainEvents.Add(new ReportClusterRemovedEvent(Id, previous));
    }

    public void SetSuggestedCluster(Guid clusterId, ConfidenceScore score)
    {
        SuggestedClusterId = clusterId;
        SuggestedConfidenceScore = score;
    }

    // ──────────────────────────────────────────────────────────────
    // Status
    // ──────────────────────────────────────────────────────────────

    public void ChangeStatus(ReportStatus newStatus)
    {
        if (newStatus == Status) return;

        var old = Status;
        Status = newStatus;
        _domainEvents.Add(new ReportStatusChangedEvent(Id, old, newStatus));
    }

    // ──────────────────────────────────────────────────────────────
    // Locking (quota overage)
    // ──────────────────────────────────────────────────────────────

    public void Lock()
    {
        if (IsLocked) return;
        IsLocked = true;
        IsOverage = true;
        _domainEvents.Add(new ReportLockedEvent(Id, OrganizationId));
    }

    public void Unlock()
    {
        IsLocked = false;
    }

    // ──────────────────────────────────────────────────────────────
    // Assignment
    // ──────────────────────────────────────────────────────────────

    public void AssignTo(string? userId) => AssignedTo = userId;
}
