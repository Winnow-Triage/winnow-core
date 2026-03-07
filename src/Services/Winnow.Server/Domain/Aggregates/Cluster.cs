using Winnow.Server.Domain.Events;
using Winnow.Server.Domain.ValueObjects;

namespace Winnow.Server.Domain.Aggregates;

/// <summary>
/// Represents a group of related bug reports.
/// Clusters own their report membership — AddReport/RemoveReport live here.
/// Invariant: a Cluster must always contain at least one report.
/// </summary>
public class Cluster : IAggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    public void ClearDomainEvents() => _domainEvents.Clear();

    private readonly List<Guid> _reportIds = [];
    public IReadOnlyList<Guid> ReportIds => _reportIds.AsReadOnly();
    public int ReportCount => _reportIds.Count;

    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid OrganizationId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public string? Title { get; private set; }
    public string? Summary { get; private set; }
    public int? CriticalityScore { get; private set; }
    public string? CriticalityReasoning { get; private set; }
    public DateTime? LastSummarizedAt { get; private set; }

    // The centroid is computed externally by IVectorCalculator and stored here
    public float[]? Centroid { get; private set; }

    public ClusterStatus Status { get; private set; }
    public string? AssignedTo { get; private set; }

    // Merge suggestion from the AI
    public Guid? SuggestedMergeClusterId { get; private set; }
    public ConfidenceScore? SuggestedMergeConfidenceScore { get; private set; }

    // Private EF constructor
    private Cluster() { }

    public Cluster(Guid projectId, Guid organizationId, Guid firstReportId)
    {
        if (projectId == Guid.Empty)
            throw new ArgumentException("Project ID is required.", nameof(projectId));
        if (organizationId == Guid.Empty)
            throw new ArgumentException("Organization ID is required.", nameof(organizationId));
        if (firstReportId == Guid.Empty)
            throw new ArgumentException("A cluster must be created with at least one report.", nameof(firstReportId));

        Id = Guid.NewGuid();
        ProjectId = projectId;
        OrganizationId = organizationId;
        Status = ClusterStatus.Open;
        CreatedAt = DateTime.UtcNow;

        // Clusters are born with their first report — enforces the invariant from creation
        _reportIds.Add(firstReportId);
        _domainEvents.Add(new ReportAddedToClusterEvent(Id, firstReportId));
    }

    // ──────────────────────────────────────────────────────────────
    // Report membership (the core invariant lives here)
    // ──────────────────────────────────────────────────────────────

    public void AddReport(Guid reportId)
    {
        if (reportId == Guid.Empty)
            throw new ArgumentException("Report ID must not be empty.", nameof(reportId));
        if (_reportIds.Contains(reportId))
            return; // idempotent

        _reportIds.Add(reportId);
        _domainEvents.Add(new ReportAddedToClusterEvent(Id, reportId));
    }

    public void RemoveReport(Guid reportId)
    {
        if (!_reportIds.Contains(reportId))
            return; // idempotent

        if (_reportIds.Count == 1)
            throw new InvalidOperationException(
                "Cannot remove the last report from a cluster. A cluster must always contain at least one report.");

        _reportIds.Remove(reportId);
    }

    // ──────────────────────────────────────────────────────────────
    // Centroid (set externally after IVectorCalculator recalculates)
    // ──────────────────────────────────────────────────────────────

    public void UpdateCentroid(float[] centroid)
    {
        ArgumentNullException.ThrowIfNull(centroid);
        Centroid = centroid;
    }

    // ──────────────────────────────────────────────────────────────
    // AI Summary
    // ──────────────────────────────────────────────────────────────

    public void SetSummary(string title, string summary, int criticalityScore, string reasoning)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Cluster title is required.", nameof(title));
        if (string.IsNullOrWhiteSpace(summary))
            throw new ArgumentException("Cluster summary is required.", nameof(summary));
        if (criticalityScore is < 1 or > 10)
            throw new ArgumentOutOfRangeException(nameof(criticalityScore), "Criticality score must be between 1 and 10.");

        Title = title;
        Summary = summary;
        CriticalityScore = criticalityScore;
        CriticalityReasoning = reasoning;
        LastSummarizedAt = DateTime.UtcNow;

        _domainEvents.Add(new ClusterSummarizedEvent(Id, OrganizationId, criticalityScore));
    }

    // ──────────────────────────────────────────────────────────────
    // Merge suggestion
    // ──────────────────────────────────────────────────────────────

    public void SuggestMerge(Guid targetClusterId, ConfidenceScore score)
    {
        if (targetClusterId == Guid.Empty)
            throw new ArgumentException("Target cluster ID must not be empty.", nameof(targetClusterId));
        if (targetClusterId == Id)
            throw new ArgumentException("A cluster cannot be suggested to merge with itself.", nameof(targetClusterId));

        SuggestedMergeClusterId = targetClusterId;
        SuggestedMergeConfidenceScore = score;
        _domainEvents.Add(new ClusterMergeSuggestedEvent(Id, targetClusterId, score.Value));
    }

    // ──────────────────────────────────────────────────────────────
    // Status
    // ──────────────────────────────────────────────────────────────

    public void ChangeStatus(ClusterStatus newStatus)
    {
        if (newStatus == Status) return;

        var old = Status;
        Status = newStatus;
        _domainEvents.Add(new ClusterStatusChangedEvent(Id, old, newStatus));
    }

    // ──────────────────────────────────────────────────────────────
    // Assignment
    // ──────────────────────────────────────────────────────────────

    public void AssignTo(string? userId) => AssignedTo = userId;
}
