using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Winnow.API.Domain.Clusters;
using Winnow.API.Domain.Clusters.ValueObjects;
using Winnow.API.Domain.Common;
using Winnow.API.Domain.Organizations;
using Winnow.API.Domain.Organizations.ValueObjects;
using Winnow.API.Domain.Projects;
using Winnow.API.Domain.Reports;
using Winnow.API.Domain.Reports.ValueObjects;
using Winnow.API.Domain.Services;
using Winnow.API.Infrastructure.Identity;
using Winnow.API.Infrastructure.Persistence;
using Winnow.API.Services.Ai;
using Winnow.Clustering.Infrastructure.Scheduling;
using Xunit;
using Moq;
using MassTransit;
using Winnow.Contracts;

namespace Winnow.API.Tests.Integration;

[Collection("PostgresCollection")]
public class ClusterRefinementJobTests(PostgresFixture fixture) : IAsyncLifetime
{
    private readonly WinnowTestApp _app = new(fixture);
    private Guid _projectId;
    private Guid _organizationId;

    /// <summary>Creates a 384-dimensional vector with the first elements set, rest zero.</summary>
    private static float[] MakeVector(params float[] seed)
    {
        var v = new float[384];
        for (var i = 0; i < seed.Length && i < v.Length; i++) v[i] = seed[i];
        return v;
    }

    public async Task InitializeAsync()
    {
        await _app.ResetDatabaseAsync();
        var client = _app.CreateClient(); // Just to trigger app creation
        using var scope = _app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WinnowDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        await db.Database.EnsureCreatedAsync();

        // Setup Test Data
        var organization = new Organization("Test Org", new Email("test@example.com"), SubscriptionPlan.Free);
        _organizationId = organization.Id;
        db.Organizations.Add(organization);

        var userId = "test-user-id";
        var user = new ApplicationUser
        {
            Id = userId,
            UserName = "test@example.com",
            Email = "test@example.com",
            FullName = "Test User",
            CreatedAt = DateTime.UtcNow
        };
        var result = await userManager.CreateAsync(user, "Password123!");
        if (!result.Succeeded)
        {
            throw new Exception($"Failed to create test user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        var ownerRole = await db.Roles.FirstOrDefaultAsync(r => r.Name == "Owner" && r.OrganizationId == null);
        if (ownerRole == null)
        {
            ownerRole = new Winnow.API.Domain.Security.Role("Owner");
            db.Roles.Add(ownerRole);
            await db.SaveChangesAsync();
        }

        var orgMember = new OrganizationMember(
            _organizationId,
            userId,
            ownerRole.Id);
        db.OrganizationMembers.Add(orgMember);

        _projectId = Guid.NewGuid();
        var project = new Project(
            _organizationId,
            "Test Project",
            userId,
            "test-hash",
            _projectId);
        db.Projects.Add(project);

        await db.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private ClusterRefinementJob CreateJob(IServiceScope scope)
    {
        var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();
        var publishEndpoint = new Mock<IPublishEndpoint>();
        return new ClusterRefinementJob(scopeFactory, publishEndpoint.Object, NullLogger<ClusterRefinementJob>.Instance);
    }

    [Fact]
    public async Task ProcessProjectAsync_HealsMissingEmbeddings()
    {
        using var scope = _app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WinnowDbContext>();

        var report = new Report(_projectId, _organizationId, "No Embedding Report", "Needs embedding");
        db.Reports.Add(report);
        await db.SaveChangesAsync();

        var job = CreateJob(scope);
        await job.ProcessProjectAsync(db, _projectId,
            scope.ServiceProvider.GetRequiredService<IEmbeddingService>(),
            scope.ServiceProvider.GetRequiredService<IDuplicateChecker>(),
            scope.ServiceProvider.GetRequiredService<INegativeMatchCache>(),
            scope.ServiceProvider.GetRequiredService<IVectorCalculator>(),
            scope.ServiceProvider.GetRequiredService<IClusterService>(),
            CancellationToken.None);

        db.ChangeTracker.Clear();
        var updatedReport = await db.Reports.FindAsync(report.Id);
        Assert.NotNull(updatedReport);
        Assert.NotNull(updatedReport.Embedding);
        Assert.NotNull(updatedReport.ClusterId); // It also creates a cluster for orphans!
    }

    [Fact]
    public async Task ProcessProjectAsync_MergesOrphanIntoCluster_HighConfidence()
    {
        using var scope = _app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WinnowDbContext>();

        var dummyReport = new Report(_projectId, _organizationId, "Dummy", "Dummy", embedding: MakeVector(1.0f, 0.0f));
        db.Reports.Add(dummyReport);
        var cluster = new Cluster(_projectId, _organizationId, dummyReport.Id);
        cluster.UpdateCentroid(MakeVector(1.0f, 0.0f));
        db.Clusters.Add(cluster);

        var report = new Report(_projectId, _organizationId, "Matched Report", "Matches cluster exactly", embedding: MakeVector(1.0f, 0.0f));
        db.Reports.Add(report);
        await db.SaveChangesAsync();

        var job = CreateJob(scope);
        await job.ProcessProjectAsync(db, _projectId,
            scope.ServiceProvider.GetRequiredService<IEmbeddingService>(),
            scope.ServiceProvider.GetRequiredService<IDuplicateChecker>(),
            scope.ServiceProvider.GetRequiredService<INegativeMatchCache>(),
            scope.ServiceProvider.GetRequiredService<IVectorCalculator>(),
            scope.ServiceProvider.GetRequiredService<IClusterService>(),
            CancellationToken.None);

        db.ChangeTracker.Clear();
        var updatedReport = await db.Reports.FindAsync(report.Id);
        Assert.Equal(cluster.Id, updatedReport?.ClusterId);
        Assert.Equal(ReportStatus.Duplicate, updatedReport?.Status);
    }

    [Fact]
    public async Task ProcessProjectAsync_SuggestsOrphanIntoCluster()
    {
        using var scope = _app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WinnowDbContext>();

        var dummyReport = new Report(_projectId, _organizationId, "Dummy", "Dummy", embedding: MakeVector(1.0f, 0.0f, 0.0f));
        db.Reports.Add(dummyReport);
        var cluster = new Cluster(_projectId, _organizationId, dummyReport.Id);
        cluster.UpdateCentroid(MakeVector(1.0f, 0.0f, 0.0f));
        db.Clusters.Add(cluster);

        var report = new Report(_projectId, _organizationId, "Similar Report", "Matches cluster contextually", embedding: MakeVector(1.0f, 1.732f, 0.0f));
        db.Reports.Add(report);
        await db.SaveChangesAsync();

        var job = CreateJob(scope);
        await job.ProcessProjectAsync(db, _projectId,
            scope.ServiceProvider.GetRequiredService<IEmbeddingService>(),
            scope.ServiceProvider.GetRequiredService<IDuplicateChecker>(),
            scope.ServiceProvider.GetRequiredService<INegativeMatchCache>(),
            scope.ServiceProvider.GetRequiredService<IVectorCalculator>(),
            scope.ServiceProvider.GetRequiredService<IClusterService>(),
            CancellationToken.None);

        db.ChangeTracker.Clear();
        var updatedReport = await db.Reports.FindAsync(report.Id);

        // Should not be hard-merged
        Assert.Null(updatedReport?.ClusterId);
        Assert.NotEqual(ReportStatus.Duplicate, updatedReport?.Status);

        // Should be suggested
        Assert.Equal(cluster.Id, updatedReport?.SuggestedClusterId);
        Assert.NotNull(updatedReport?.SuggestedConfidenceScore);
    }

    private class StubDuplicateChecker(bool isDuplicate) : IDuplicateChecker
    {
        public Task<bool> AreDuplicatesAsync(string title1, string message1, string title2, string message2, CancellationToken ct = default)
            => Task.FromResult(isDuplicate);
    }

    [Fact]
    public async Task ProcessProjectAsync_AIConfirmsDuplicate_AndMerges()
    {
        using var scope = _app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WinnowDbContext>();

        var dummyReport = new Report(_projectId, _organizationId, "Dummy", "Dummy", embedding: MakeVector(1.0f, 0.0f, 0.0f));
        db.Reports.Add(dummyReport);
        var cluster = new Cluster(_projectId, _organizationId, dummyReport.Id);
        cluster.UpdateCentroid(MakeVector(1.0f, 0.0f, 0.0f));
        db.Clusters.Add(cluster);

        // Need a representative report in the cluster for duplicate checking
        var repReport = new Report(_projectId, _organizationId, "Rep Report", "Rep Message", embedding: MakeVector(1.0f, 0.0f, 0.0f));
        repReport.AssignToCluster(cluster.Id);
        db.Reports.Add(repReport);

        var report = new Report(_projectId, _organizationId, "Duplicate Report", "Matches cluster", embedding: MakeVector(1.0f, 1.0f, 0.0f));
        db.Reports.Add(report);
        await db.SaveChangesAsync();

        var job = CreateJob(scope);
        await job.ProcessProjectAsync(db, _projectId,
            scope.ServiceProvider.GetRequiredService<IEmbeddingService>(),
            new StubDuplicateChecker(isDuplicate: true),
            scope.ServiceProvider.GetRequiredService<INegativeMatchCache>(),
            scope.ServiceProvider.GetRequiredService<IVectorCalculator>(),
            scope.ServiceProvider.GetRequiredService<IClusterService>(),
            CancellationToken.None);

        db.ChangeTracker.Clear();
        var updatedReport = await db.Reports.FindAsync(report.Id);

        Assert.Equal(cluster.Id, updatedReport?.ClusterId);
        Assert.Equal(ReportStatus.Duplicate, updatedReport?.Status);
    }

    [Fact]
    public async Task ProcessProjectAsync_AIRejectsDuplicate_AndSuggestsInstead()
    {
        using var scope = _app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WinnowDbContext>();

        var dummyReport = new Report(_projectId, _organizationId, "Dummy", "Dummy", embedding: MakeVector(1.0f, 0.0f, 0.0f));
        db.Reports.Add(dummyReport);
        var cluster = new Cluster(_projectId, _organizationId, dummyReport.Id);
        cluster.UpdateCentroid(MakeVector(1.0f, 0.0f, 0.0f));
        db.Clusters.Add(cluster);

        var repReport = new Report(_projectId, _organizationId, "Rep Report", "Rep Message", embedding: MakeVector(1.0f, 0.0f, 0.0f));
        repReport.AssignToCluster(cluster.Id);
        db.Reports.Add(repReport);

        var report = new Report(_projectId, _organizationId, "Rejected Report", "Matches cluster vectors but AI says no", embedding: MakeVector(1.0f, 1.0f, 0.0f));
        db.Reports.Add(report);
        await db.SaveChangesAsync();

        var job = CreateJob(scope);
        await job.ProcessProjectAsync(db, _projectId,
            scope.ServiceProvider.GetRequiredService<IEmbeddingService>(),
            new StubDuplicateChecker(isDuplicate: false), // AI rejects
            scope.ServiceProvider.GetRequiredService<INegativeMatchCache>(),
            scope.ServiceProvider.GetRequiredService<IVectorCalculator>(),
            scope.ServiceProvider.GetRequiredService<IClusterService>(),
            CancellationToken.None);

        db.ChangeTracker.Clear();
        var updatedReport = await db.Reports.FindAsync(report.Id);

        // Should not be hard-merged because AI rejected
        Assert.NotEqual(cluster.Id, updatedReport?.ClusterId);

        // Should be suggested instead
        Assert.Equal(cluster.Id, updatedReport?.SuggestedClusterId);
    }

    [Fact]
    public async Task ProcessProjectAsync_AutoMergesClusters()
    {
        using var scope = _app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WinnowDbContext>();

        var dummyReport1 = new Report(_projectId, _organizationId, "Dummy 1", "Dummy 1", embedding: MakeVector(1.0f, 0.0f, 0.0f));
        db.Reports.Add(dummyReport1);
        var cluster1 = new Cluster(_projectId, _organizationId, dummyReport1.Id);
        cluster1.UpdateCentroid(MakeVector(1.0f, 0.0f, 0.0f));
        db.Clusters.Add(cluster1);

        var dummyReport2 = new Report(_projectId, _organizationId, "Dummy 2", "Dummy 2", embedding: MakeVector(1.0f, 0.0f, 0.0f));
        db.Reports.Add(dummyReport2);
        var cluster2 = new Cluster(_projectId, _organizationId, dummyReport2.Id);
        cluster2.UpdateCentroid(MakeVector(1.0f, 0.0f, 0.0f)); // Exact same centroid => distance 0
        db.Clusters.Add(cluster2);
        await db.SaveChangesAsync();

        var job = CreateJob(scope);
        await job.ProcessProjectAsync(db, _projectId,
            scope.ServiceProvider.GetRequiredService<IEmbeddingService>(),
            scope.ServiceProvider.GetRequiredService<IDuplicateChecker>(),
            scope.ServiceProvider.GetRequiredService<INegativeMatchCache>(),
            scope.ServiceProvider.GetRequiredService<IVectorCalculator>(),
            scope.ServiceProvider.GetRequiredService<IClusterService>(),
            CancellationToken.None);

        db.ChangeTracker.Clear();

        // One cluster should be deleted (merged into the other)
        var remainingClusters = await db.Clusters.Where(c => c.ProjectId == _projectId).ToListAsync();
        Assert.Single(remainingClusters);
    }

    [Fact]
    public async Task ProcessProjectAsync_SuggestsClusterMerge()
    {
        using var scope = _app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WinnowDbContext>();

        var dummyReport1 = new Report(_projectId, _organizationId, "Dummy 1", "Dummy 1", embedding: MakeVector(1.0f, 0.0f, 0.0f));
        db.Reports.Add(dummyReport1);
        var cluster1 = new Cluster(_projectId, _organizationId, dummyReport1.Id);
        cluster1.UpdateCentroid(MakeVector(1.0f, 0.0f, 0.0f));
        db.Clusters.Add(cluster1);

        var dummyReport2 = new Report(_projectId, _organizationId, "Dummy 2", "Dummy 2", embedding: MakeVector(1.0f, 1.0f, 0.0f));
        db.Reports.Add(dummyReport2);
        var cluster2 = new Cluster(_projectId, _organizationId, dummyReport2.Id);
        // Sim ~0.707 => Dist ~0.293 (between 0.25 and 0.45) => Suggested merge
        cluster2.UpdateCentroid(MakeVector(1.0f, 1.0f, 0.0f));
        db.Clusters.Add(cluster2);
        await db.SaveChangesAsync();

        var job = CreateJob(scope);
        await job.ProcessProjectAsync(db, _projectId,
            scope.ServiceProvider.GetRequiredService<IEmbeddingService>(),
            scope.ServiceProvider.GetRequiredService<IDuplicateChecker>(),
            scope.ServiceProvider.GetRequiredService<INegativeMatchCache>(),
            scope.ServiceProvider.GetRequiredService<IVectorCalculator>(),
            scope.ServiceProvider.GetRequiredService<IClusterService>(),
            CancellationToken.None);

        db.ChangeTracker.Clear();

        // Both clusters remain
        var remainingClusters = await db.Clusters.Where(c => c.ProjectId == _projectId).ToListAsync();
        Assert.Equal(2, remainingClusters.Count);

        // One should have a suggested merge
        var suggested = remainingClusters.FirstOrDefault(c => c.SuggestedMergeClusterId != null);
        Assert.NotNull(suggested);
        Assert.NotNull(suggested?.SuggestedMergeConfidenceScore);
    }

    [Fact]
    public async Task ProcessProjectAsync_SkipsDuplicateCheck_IfNegativeCacheMatches()
    {
        using var scope = _app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WinnowDbContext>();

        var dummyReport = new Report(_projectId, _organizationId, "Dummy", "Dummy", embedding: MakeVector(1.0f, 0.0f, 0.0f));
        db.Reports.Add(dummyReport);
        var cluster = new Cluster(_projectId, _organizationId, dummyReport.Id);
        cluster.UpdateCentroid(MakeVector(1.0f, 0.0f, 0.0f));
        db.Clusters.Add(cluster);

        var repReport = new Report(_projectId, _organizationId, "Rep Report", "Rep Message", embedding: MakeVector(1.0f, 0.0f, 0.0f));
        repReport.AssignToCluster(cluster.Id);
        db.Reports.Add(repReport);

        var report = new Report(_projectId, _organizationId, "Duplicate Report", "Matches cluster, but previously marked as not a duplicate by user", embedding: MakeVector(1.0f, 1.0f, 0.0f));
        db.Reports.Add(report);
        await db.SaveChangesAsync();

        // Mock negative cache to return true for this pair
        var negativeCache = scope.ServiceProvider.GetRequiredService<INegativeMatchCache>();
        negativeCache.MarkAsMismatch("default", report.Id, repReport.Id);

        var job = CreateJob(scope);
        await job.ProcessProjectAsync(db, _projectId,
            scope.ServiceProvider.GetRequiredService<IEmbeddingService>(),
            new StubDuplicateChecker(isDuplicate: true),
            negativeCache,
            scope.ServiceProvider.GetRequiredService<IVectorCalculator>(),
            scope.ServiceProvider.GetRequiredService<IClusterService>(),
            CancellationToken.None);

        db.ChangeTracker.Clear();
        var updatedReport = await db.Reports.FindAsync(report.Id);

        // Should not be hard-merged due to cache
        Assert.NotEqual(cluster.Id, updatedReport?.ClusterId);

        // Should be suggested
        Assert.Equal(cluster.Id, updatedReport?.SuggestedClusterId);
    }

    [Fact]
    public async Task ProcessProjectAsync_MergesIfRepresentativeIsMissing()
    {
        using var scope = _app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WinnowDbContext>();

        var dummyReport = new Report(_projectId, _organizationId, "Dummy", "Dummy", embedding: MakeVector(1.0f, 0.0f, 0.0f));
        db.Reports.Add(dummyReport);
        var cluster = new Cluster(_projectId, _organizationId, dummyReport.Id);
        cluster.UpdateCentroid(MakeVector(1.0f, 0.0f, 0.0f));
        db.Clusters.Add(cluster);
        // No OTHER reports in this cluster except the the one that created it (representative)
        // Wait, the test says "Empty Cluster". But in the new design, a cluster MUST have one report.
        // I'll keep the invariant: cluster has dummyReport.

        var report = new Report(_projectId, _organizationId, "Duplicate Report", "Matches cluster", embedding: MakeVector(1.0f, 1.0f, 0.0f));
        db.Reports.Add(report);
        await db.SaveChangesAsync();

        var job = CreateJob(scope);
        await job.ProcessProjectAsync(db, _projectId,
            scope.ServiceProvider.GetRequiredService<IEmbeddingService>(),
            new StubDuplicateChecker(isDuplicate: false), // AI would reject, but there's no representative!
            scope.ServiceProvider.GetRequiredService<INegativeMatchCache>(),
            scope.ServiceProvider.GetRequiredService<IVectorCalculator>(),
            scope.ServiceProvider.GetRequiredService<IClusterService>(),
            CancellationToken.None);

        db.ChangeTracker.Clear();
        var updatedReport = await db.Reports.FindAsync(report.Id);

        Assert.Equal(cluster.Id, updatedReport?.ClusterId);
        Assert.Equal(ReportStatus.Duplicate, updatedReport?.Status);
    }

    [Fact]
    public async Task ExecuteAsync_RunsAndProcessesProjects()
    {
        using var scope = _app.Services.CreateScope();
        var job = CreateJob(scope);

        var db = scope.ServiceProvider.GetRequiredService<WinnowDbContext>();

        var report = new Report(_projectId, _organizationId, "Orphan Exec", "Test Exec");
        db.Reports.Add(report);
        await db.SaveChangesAsync();

        using var cts = new CancellationTokenSource();

        await job.StartAsync(cts.Token);

        // Yield to let the background task start and run its loop
        await Task.Delay(2000);

        // Cancel to break the loop Delay
        await cts.CancelAsync();

        try
        {
            await job.StopAsync(CancellationToken.None);
        }
        catch (OperationCanceledException) { /* Ignored */ }

        db.ChangeTracker.Clear();
        var updatedReport = await db.Reports.FindAsync(report.Id);

        Assert.NotNull(updatedReport?.Embedding);
        Assert.NotNull(updatedReport?.ClusterId);
    }
}
