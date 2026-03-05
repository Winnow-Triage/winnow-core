using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Winnow.Server.Domain.Services;
using Winnow.Server.Entities;
using Winnow.Server.Infrastructure.Persistence;
using Winnow.Server.Infrastructure.Scheduling;
using Winnow.Server.Services.Ai;
using Xunit;

namespace Winnow.Server.Tests.Integration;

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
        _organizationId = Guid.NewGuid();
        var organization = new Organization { Id = _organizationId, Name = "Test Org", SubscriptionTier = "free", CreatedAt = DateTime.UtcNow };
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

        var orgMember = new OrganizationMember
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            OrganizationId = _organizationId,
            Role = "owner",
            JoinedAt = DateTime.UtcNow
        };
        db.OrganizationMembers.Add(orgMember);

        _projectId = Guid.NewGuid();
        var project = new Project
        {
            Id = _projectId,
            Name = "Test Project",
            OrganizationId = _organizationId,
            OwnerId = userId,
            ApiKeyHash = "test-hash",
            CreatedAt = DateTime.UtcNow
        };
        db.Projects.Add(project);

        await db.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private ClusterRefinementJob CreateJob(IServiceScope scope)
    {
        var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();
        return new ClusterRefinementJob(scopeFactory, NullLogger<ClusterRefinementJob>.Instance);
    }

    [Fact]
    public async Task ProcessProjectAsync_HealsMissingEmbeddings()
    {
        using var scope = _app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WinnowDbContext>();

        var report = new Report
        {
            Id = Guid.NewGuid(),
            ProjectId = _projectId,
            OrganizationId = _organizationId,
            Title = "No Embedding Report",
            Message = "Needs embedding",
            ClusterId = null,
            Embedding = null,
            CreatedAt = DateTime.UtcNow,
            Status = "New"
        };
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

        var cluster = new Cluster
        {
            Id = Guid.NewGuid(),
            ProjectId = _projectId,
            OrganizationId = _organizationId,
            Title = "Existing Cluster",
            Status = "Open",
            Centroid = MakeVector(1.0f, 0.0f)
        };
        db.Clusters.Add(cluster);

        var report = new Report
        {
            Id = Guid.NewGuid(),
            ProjectId = _projectId,
            OrganizationId = _organizationId,
            Title = "Matched Report",
            Message = "Matches cluster exactly",
            ClusterId = null,
            Embedding = MakeVector(1.0f, 0.0f), // Distance 0 to centroid -> Hard merge
            CreatedAt = DateTime.UtcNow,
            Status = "New"
        };
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
        Assert.Equal("Duplicate", updatedReport?.Status);
    }

    [Fact]
    public async Task ProcessProjectAsync_SuggestsOrphanIntoCluster()
    {
        using var scope = _app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WinnowDbContext>();

        var cluster = new Cluster
        {
            Id = Guid.NewGuid(),
            ProjectId = _projectId,
            OrganizationId = _organizationId,
            Title = "Existing Cluster",
            Status = "Open",
            Centroid = MakeVector(1.0f, 0.0f, 0.0f)
        };
        db.Clusters.Add(cluster);

        var report = new Report
        {
            Id = Guid.NewGuid(),
            ProjectId = _projectId,
            OrganizationId = _organizationId,
            Title = "Similar Report",
            Message = "Matches cluster contextually",
            ClusterId = null,
            // A vector that gives Cosine Similarity ~0.5 => Distance ~0.5
            // e.g., A=[1,0], B=[1, 1.732] -> dot=1, |A|=1, |B|=2 -> sim=0.5 -> dist=0.5
            Embedding = MakeVector(1.0f, 1.732f, 0.0f),
            CreatedAt = DateTime.UtcNow,
            Status = "New"
        };
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
        Assert.NotEqual("Duplicate", updatedReport?.Status);

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

        var cluster = new Cluster
        {
            Id = Guid.NewGuid(),
            ProjectId = _projectId,
            OrganizationId = _organizationId,
            Title = "Existing Cluster",
            Status = "Open",
            Centroid = MakeVector(1.0f, 0.0f, 0.0f)
        };
        db.Clusters.Add(cluster);

        // Need a representative report in the cluster for duplicate checking
        var repReport = new Report
        {
            Id = Guid.NewGuid(),
            ProjectId = _projectId,
            OrganizationId = _organizationId,
            Title = "Rep Report",
            Message = "Rep Message",
            ClusterId = cluster.Id,
            Embedding = MakeVector(1.0f, 0.0f, 0.0f),
            CreatedAt = DateTime.UtcNow,
            Status = "Grouped"
        };
        db.Reports.Add(repReport);

        var report = new Report
        {
            Id = Guid.NewGuid(),
            ProjectId = _projectId,
            OrganizationId = _organizationId,
            Title = "Duplicate Report",
            Message = "Matches cluster",
            ClusterId = null,
            // A vector that gives Cosine Similarity ~0.707 => Distance ~0.293
            // e.g., A=[1,0], B=[1, 1] -> dot=1, |A|=1, |B|=sqrt(2) -> sim=0.707 -> dist=0.293
            Embedding = MakeVector(1.0f, 1.0f, 0.0f),
            CreatedAt = DateTime.UtcNow,
            Status = "New"
        };
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
        Assert.Equal("Duplicate", updatedReport?.Status);
    }

    [Fact]
    public async Task ProcessProjectAsync_AIRejectsDuplicate_AndSuggestsInstead()
    {
        using var scope = _app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WinnowDbContext>();

        var cluster = new Cluster
        {
            Id = Guid.NewGuid(),
            ProjectId = _projectId,
            OrganizationId = _organizationId,
            Title = "Existing Cluster",
            Status = "Open",
            Centroid = MakeVector(1.0f, 0.0f, 0.0f)
        };
        db.Clusters.Add(cluster);

        var repReport = new Report
        {
            Id = Guid.NewGuid(),
            ProjectId = _projectId,
            OrganizationId = _organizationId,
            Title = "Rep Report",
            Message = "Rep Message",
            ClusterId = cluster.Id,
            Embedding = MakeVector(1.0f, 0.0f, 0.0f),
            CreatedAt = DateTime.UtcNow,
            Status = "Grouped"
        };
        db.Reports.Add(repReport);

        var report = new Report
        {
            Id = Guid.NewGuid(),
            ProjectId = _projectId,
            OrganizationId = _organizationId,
            Title = "Rejected Report",
            Message = "Matches cluster vectors but AI says no",
            ClusterId = null,
            Embedding = MakeVector(1.0f, 1.0f, 0.0f), // Dist ~0.293 
            CreatedAt = DateTime.UtcNow,
            Status = "New"
        };
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

        var cluster1 = new Cluster
        {
            Id = Guid.NewGuid(),
            ProjectId = _projectId,
            OrganizationId = _organizationId,
            Title = "Cluster 1",
            Status = "Open",
            Centroid = MakeVector(1.0f, 0.0f, 0.0f)
        };
        db.Clusters.Add(cluster1);

        var cluster2 = new Cluster
        {
            Id = Guid.NewGuid(),
            ProjectId = _projectId,
            OrganizationId = _organizationId,
            Title = "Cluster 2",
            Status = "Open",
            Centroid = MakeVector(1.0f, 0.0f, 0.0f) // Exact same centroid => distance 0
        };
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

        var cluster1 = new Cluster
        {
            Id = Guid.NewGuid(),
            ProjectId = _projectId,
            OrganizationId = _organizationId,
            Title = "Cluster 1",
            Status = "Open",
            Centroid = MakeVector(1.0f, 0.0f, 0.0f)
        };
        db.Clusters.Add(cluster1);

        var cluster2 = new Cluster
        {
            Id = Guid.NewGuid(),
            ProjectId = _projectId,
            OrganizationId = _organizationId,
            Title = "Cluster 2",
            Status = "Open",
            // Sim ~0.707 => Dist ~0.293 (between 0.25 and 0.45) => Suggested merge
            Centroid = MakeVector(1.0f, 1.0f, 0.0f)
        };
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

        var cluster = new Cluster
        {
            Id = Guid.NewGuid(),
            ProjectId = _projectId,
            OrganizationId = _organizationId,
            Title = "Existing Cluster",
            Status = "Open",
            Centroid = MakeVector(1.0f, 0.0f, 0.0f)
        };
        db.Clusters.Add(cluster);

        var repReport = new Report
        {
            Id = Guid.NewGuid(),
            ProjectId = _projectId,
            OrganizationId = _organizationId,
            Title = "Rep Report",
            Message = "Rep Message",
            ClusterId = cluster.Id,
            Embedding = MakeVector(1.0f, 0.0f, 0.0f),
            CreatedAt = DateTime.UtcNow,
            Status = "Grouped"
        };
        db.Reports.Add(repReport);

        var report = new Report
        {
            Id = Guid.NewGuid(),
            ProjectId = _projectId,
            OrganizationId = _organizationId,
            Title = "Duplicate Report",
            Message = "Matches cluster, but previously marked as not a duplicate by user",
            ClusterId = null,
            Embedding = MakeVector(1.0f, 1.0f, 0.0f), // Dist ~0.293 
            CreatedAt = DateTime.UtcNow,
            Status = "New"
        };
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

        var cluster = new Cluster
        {
            Id = Guid.NewGuid(),
            ProjectId = _projectId,
            OrganizationId = _organizationId,
            Title = "Empty Cluster",
            Status = "Open",
            Centroid = MakeVector(1.0f, 0.0f, 0.0f)
        };
        db.Clusters.Add(cluster);
        // No reports in this cluster

        var report = new Report
        {
            Id = Guid.NewGuid(),
            ProjectId = _projectId,
            OrganizationId = _organizationId,
            Title = "Duplicate Report",
            Message = "Matches cluster",
            ClusterId = null,
            Embedding = MakeVector(1.0f, 1.0f, 0.0f), // Dist ~0.293 
            CreatedAt = DateTime.UtcNow,
            Status = "New"
        };
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
        Assert.Equal("Duplicate", updatedReport?.Status);
    }

    [Fact]
    public async Task ExecuteAsync_RunsAndProcessesProjects()
    {
        using var scope = _app.Services.CreateScope();
        var job = CreateJob(scope);

        var db = scope.ServiceProvider.GetRequiredService<WinnowDbContext>();

        var report = new Report
        {
            Id = Guid.NewGuid(),
            ProjectId = _projectId,
            OrganizationId = _organizationId,
            Title = "Orphan Exec",
            Message = "Test Exec",
            ClusterId = null,
            Embedding = null,
            CreatedAt = DateTime.UtcNow,
            Status = "New"
        };
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
