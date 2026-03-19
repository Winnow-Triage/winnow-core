using Winnow.Server.Domain.Common;
using Winnow.Server.Domain.Core;
using Winnow.Server.Domain.Reports.Events;
using Winnow.Server.Domain.Reports.ValueObjects;

namespace Winnow.Server.Domain.Reports;

/// <summary>
/// Represents a bug report submitted to Winnow.
/// A Report can exist without a Cluster (floating free), or be assigned to one.
/// </summary>
public class Report : IAggregateRoot
{
    public const int ExpectedEmbeddingLength = 384;
    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    public void ClearDomainEvents() => _domainEvents.Clear();

    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid OrganizationId { get; private set; }

    private readonly List<Guid> _assetIds = [];
    public IReadOnlyCollection<Guid> Assets => _assetIds.AsReadOnly();

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

    // Overage / billing stateEmbedding
    public bool IsOverage { get; private set; }
    public bool IsLocked { get; private set; }

    public string? AssignedTo { get; private set; }
    public Uri? ExternalUrl { get; private set; }

    // Infrastructure/Metadata fields
    public string? Metadata { get; private set; }
    public string? Screenshot { get; private set; }
    public bool IsToxic { get; private set; }

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
        Uri? externalUrl = null,
        bool isOverage = false,
        bool isLocked = false)
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
        Status = ReportStatus.Open;
        IsOverage = isOverage;
        IsLocked = isLocked;
        CreatedAt = DateTime.UtcNow;

        _domainEvents.Add(new ReportCreatedEvent(
            Id,
            OrganizationId,
            ProjectId,
            Title
        ));
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
        _domainEvents.Add(new ReportClusterAssignedEvent(Id, clusterId, confidenceScore));
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

    public void ClearSuggestedCluster()
    {
        SuggestedClusterId = null;
        SuggestedConfidenceScore = null;
    }

    public void SetConfidenceScore(ConfidenceScore score)
    {
        ConfidenceScore = score;
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

    public void MarkAsExported(Uri externalUrl)
    {
        ArgumentNullException.ThrowIfNull(externalUrl);
        ExternalUrl = externalUrl;
        ChangeStatus(ReportStatus.Exported);
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

    public void MarkOverage()
    {
        IsOverage = true;
    }

    public void UnmarkOverage()
    {
        IsOverage = false;
    }

    public void AdminResetOverage()
    {
        IsOverage = false;
    }

    // ──────────────────────────────────────────────────────────────
    // Assignment
    // ──────────────────────────────────────────────────────────────

    public void AssignTo(string? userId) => AssignedTo = userId;

    public void SetEmbedding(float[] embedding)
    {
        ArgumentNullException.ThrowIfNull(embedding);

        if (embedding.Length != ExpectedEmbeddingLength)
        {
            throw new ArgumentException(
                $"Expected embedding of length {ExpectedEmbeddingLength}, but got {embedding.Length}.",
                nameof(embedding));
        }

        Embedding = embedding;
    }

    public void UpdateMessage(string sanitizedMessage)
    {
        if (string.IsNullOrWhiteSpace(sanitizedMessage))
        {
            throw new ArgumentException("Sanitized message cannot be empty.", nameof(sanitizedMessage));
        }

        Message = sanitizedMessage;
    }

    public void MarkAsToxic()
    {
        if (IsToxic) return;

        IsToxic = true;
        Status = ReportStatus.Dismissed;
    }

    public void MarkAsClean()
    {
        if (!IsToxic) return;

        IsToxic = false;

        if (Status == ReportStatus.Dismissed)
        {
            Status = ReportStatus.Open;
        }
    }

    public void UpdateMetadata(string? jsonMetadata)
    {
        Metadata = jsonMetadata;
    }

    public void SetScreenshot(string screenshotKey)
    {
        Screenshot = screenshotKey;
    }
}
