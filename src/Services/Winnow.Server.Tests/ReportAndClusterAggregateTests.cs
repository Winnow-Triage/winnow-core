using Winnow.Server.Domain.Aggregates;
using Winnow.Server.Domain.Events;
using Winnow.Server.Domain.ValueObjects;

namespace Winnow.Server.Tests;

public class ReportAggregateTests
{
    private static readonly Guid SomeProject = Guid.NewGuid();
    private static readonly Guid SomeOrg = Guid.NewGuid();

    private static Report CreateReport() =>
        new(SomeProject, SomeOrg, "NullReferenceException in PaymentService", "Object reference not set.");

    // ──────────────────────────────────────────────────────────────
    // Construction invariants
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_WithValidArgs_SetsStatusToNew()
    {
        var report = CreateReport();
        Assert.Equal(ReportStatus.New, report.Status);
        Assert.Null(report.ClusterId);
    }

    [Fact]
    public void Constructor_WithEmptyTitle_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new Report(SomeProject, SomeOrg, " ", "message"));
    }

    // ──────────────────────────────────────────────────────────────
    // Cluster assignment
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void AssignToCluster_SetsClusterIdAndRaisesEvent()
    {
        var report = CreateReport();
        var clusterId = Guid.NewGuid();

        report.AssignToCluster(clusterId);

        Assert.Equal(clusterId, report.ClusterId);
        var evt = Assert.Single(report.DomainEvents.OfType<ReportClusterAssignedEvent>());
        Assert.Equal(report.Id, evt.ReportId);
        Assert.Equal(clusterId, evt.ClusterId);
    }

    [Fact]
    public void RemoveFromCluster_ClearsClusterIdAndRaisesEvent()
    {
        var report = CreateReport();
        var clusterId = Guid.NewGuid();
        report.AssignToCluster(clusterId);
        report.ClearDomainEvents();

        report.RemoveFromCluster();

        Assert.Null(report.ClusterId);
        var evt = Assert.Single(report.DomainEvents.OfType<ReportClusterRemovedEvent>());
        Assert.Equal(clusterId, evt.PreviousClusterId);
    }

    [Fact]
    public void RemoveFromCluster_WhenAlreadyUnassigned_IsIdempotent()
    {
        var report = CreateReport();

        report.RemoveFromCluster(); // should not throw or raise event

        Assert.Empty(report.DomainEvents);
    }

    // ──────────────────────────────────────────────────────────────
    // Status
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void ChangeStatus_ToDifferentStatus_RaisesEvent()
    {
        var report = CreateReport();

        report.ChangeStatus(ReportStatus.Reviewed);

        var evt = Assert.Single(report.DomainEvents.OfType<ReportStatusChangedEvent>());
        Assert.Equal(ReportStatus.New, evt.OldStatus);
        Assert.Equal(ReportStatus.Reviewed, evt.NewStatus);
    }

    [Fact]
    public void ChangeStatus_ToSameStatus_DoesNotRaiseEvent()
    {
        var report = CreateReport();

        report.ChangeStatus(ReportStatus.New);

        Assert.Empty(report.DomainEvents);
    }

    // ──────────────────────────────────────────────────────────────
    // Locking
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void Lock_SetsLockedFlagAndRaisesEvent()
    {
        var report = CreateReport();

        report.Lock();

        Assert.True(report.IsLocked);
        Assert.True(report.IsOverage);
        Assert.Single(report.DomainEvents.OfType<ReportLockedEvent>());
    }

    [Fact]
    public void Lock_CalledTwice_IsIdempotent()
    {
        var report = CreateReport();
        report.Lock();
        report.ClearDomainEvents();

        report.Lock(); // second call should not raise another event

        Assert.Empty(report.DomainEvents);
    }
}

public class ClusterAggregateTests
{
    private static readonly Guid SomeProject = Guid.NewGuid();
    private static readonly Guid SomeOrg = Guid.NewGuid();

    private static Cluster CreateCluster(Guid? firstReportId = null) =>
        new(SomeProject, SomeOrg, firstReportId ?? Guid.NewGuid());

    // ──────────────────────────────────────────────────────────────
    // Construction invariants
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_AddsFirstReportAndRaisesEvent()
    {
        var reportId = Guid.NewGuid();
        var cluster = new Cluster(SomeProject, SomeOrg, reportId);

        Assert.Single(cluster.ReportIds);
        Assert.Contains(reportId, cluster.ReportIds);
        var evt = Assert.Single(cluster.DomainEvents.OfType<ClusterReportAddedEvent>());
        Assert.Equal(reportId, evt.ReportId);
    }

    [Fact]
    public void Constructor_WithEmptyFirstReportId_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new Cluster(SomeProject, SomeOrg, Guid.Empty));
    }

    // ──────────────────────────────────────────────────────────────
    // Report membership + invariant
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void AddReport_IncreasesCountAndRaisesEvent()
    {
        var cluster = CreateCluster();
        var newReportId = Guid.NewGuid();
        cluster.ClearDomainEvents();

        cluster.AddReport(newReportId);

        Assert.Equal(2, cluster.ReportCount);
        var evt = Assert.Single(cluster.DomainEvents.OfType<ClusterReportAddedEvent>());
        Assert.Equal(newReportId, evt.ReportId);
    }

    [Fact]
    public void AddReport_DuplicateId_IsIdempotent()
    {
        var reportId = Guid.NewGuid();
        var cluster = CreateCluster(reportId);
        cluster.ClearDomainEvents();

        cluster.AddReport(reportId);

        Assert.Equal(1, cluster.ReportCount);
        Assert.Empty(cluster.DomainEvents);
    }

    [Fact]
    public void RemoveReport_WhenMoreThanOne_Succeeds()
    {
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        var cluster = CreateCluster(first);
        cluster.AddReport(second);

        cluster.RemoveReport(first);

        Assert.Equal(1, cluster.ReportCount);
        Assert.DoesNotContain(first, cluster.ReportIds);
    }

    [Fact]
    public void RemoveReport_LastReport_ThrowsInvariantViolation()
    {
        var reportId = Guid.NewGuid();
        var cluster = CreateCluster(reportId);

        Assert.Throws<InvalidOperationException>(() => cluster.RemoveReport(reportId));
    }

    // ──────────────────────────────────────────────────────────────
    // AI Summary
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void SetSummary_WithValidArgs_RaisesClusterSummarizedEvent()
    {
        var cluster = CreateCluster();
        cluster.ClearDomainEvents();

        cluster.SetSummary("Payment failures", "Multiple NREs in payment path.", 8, "High user impact.");

        Assert.Equal("Payment failures", cluster.Title);
        Assert.Equal(8, cluster.CriticalityScore);
        Assert.NotNull(cluster.LastSummarizedAt);
        var evt = Assert.Single(cluster.DomainEvents.OfType<ClusterSummarizedEvent>());
        Assert.Equal(8, evt.CriticalityScore);
    }

    [Fact]
    public void SetSummary_WithOutOfRangeCriticality_Throws()
    {
        var cluster = CreateCluster();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            cluster.SetSummary("Title", "Summary", 11, "Reasoning"));
    }

    // ──────────────────────────────────────────────────────────────
    // Merge suggestion
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void SuggestMerge_WithSelf_Throws()
    {
        var cluster = CreateCluster();

        Assert.Throws<ArgumentException>(() =>
            cluster.SuggestMerge(cluster.Id, new ConfidenceScore(0.9)));
    }

    [Fact]
    public void SuggestMerge_WithValidTarget_RaisesEvent()
    {
        var cluster = CreateCluster();
        var targetId = Guid.NewGuid();
        cluster.ClearDomainEvents();

        cluster.SuggestMerge(targetId, new ConfidenceScore(0.85));

        Assert.Equal(targetId, cluster.SuggestedMergeClusterId);
        var evt = Assert.Single(cluster.DomainEvents.OfType<ClusterMergeSuggestedEvent>());
        Assert.Equal(targetId, evt.TargetClusterId);
    }

    // ──────────────────────────────────────────────────────────────
    // Status
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void ChangeStatus_ToDifferentStatus_RaisesEvent()
    {
        var cluster = CreateCluster();
        cluster.ClearDomainEvents();

        cluster.ChangeStatus(ClusterStatus.Resolved);

        var evt = Assert.Single(cluster.DomainEvents.OfType<ClusterStatusChangedEvent>());
        Assert.Equal(ClusterStatus.Open, evt.OldStatus);
        Assert.Equal(ClusterStatus.Resolved, evt.NewStatus);
    }

    [Fact]
    public void ChangeStatus_ToSameStatus_DoesNotRaiseEvent()
    {
        var cluster = CreateCluster();
        cluster.ClearDomainEvents();

        cluster.ChangeStatus(ClusterStatus.Open);

        Assert.Empty(cluster.DomainEvents);
    }
}
