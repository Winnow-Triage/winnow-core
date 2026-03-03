using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Winnow.Server.Entities;
using Winnow.Server.Infrastructure.Persistence;
using Winnow.Server.Services.Ai;

namespace Winnow.Server.Tests.Integration;

public class CentroidRecalculationTests : IAsyncLifetime
{
    private readonly WinnowTestApp _app = new();
    private Guid _projectId;
    private Guid _organizationId;

    public async Task InitializeAsync()
    {
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

    public async Task DisposeAsync()
    {
        await _app.ResetDatabaseAsync();
        _app.Dispose();
    }

    [Fact]
    public async Task UngroupReport_UpdatesCentroid()
    {
        var client = _app.CreateClient();
        using var scope = _app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WinnowDbContext>();

        // 1. Create a cluster with two reports
        var cluster = new Cluster
        {
            Id = Guid.NewGuid(),
            ProjectId = _projectId,
            OrganizationId = _organizationId,
            Title = "Test Cluster",
            Status = "Open",
            Centroid = [1.0f, 0.0f] // Initial dummy centroid
        };
        db.Clusters.Add(cluster);

        var report1 = new Report
        {
            Id = Guid.NewGuid(),
            ProjectId = _projectId,
            OrganizationId = _organizationId,
            Title = "Report 1",
            Message = "Message 1",
            ClusterId = cluster.Id,
            Embedding = [1.0f, 0.0f],
            CreatedAt = DateTime.UtcNow,
            Status = "Grouped"
        };
        var report2 = new Report
        {
            Id = Guid.NewGuid(),
            ProjectId = _projectId,
            OrganizationId = _organizationId,
            Title = "Report 2",
            Message = "Message 2",
            ClusterId = cluster.Id,
            Embedding = [0.0f, 1.0f],
            CreatedAt = DateTime.UtcNow,
            Status = "Grouped"
        };
        db.Reports.AddRange(report1, report2);
        await db.SaveChangesAsync();

        // 2. Initial centroid should be average: [0.5, 0.5]
        var clusterService = scope.ServiceProvider.GetRequiredService<IClusterService>();
        await clusterService.RecalculateCentroidAsync(cluster.Id);
        await db.SaveChangesAsync();

        var updatedCluster = await db.Clusters.FindAsync(cluster.Id);
        Assert.NotNull(updatedCluster);
        Assert.NotNull(updatedCluster.Centroid);
        Assert.Equal(0.5f, updatedCluster.Centroid![0], 3);
        Assert.Equal(0.5f, updatedCluster.Centroid![1], 3);

        // Verify confidence scores are updated
        var r1 = await db.Reports.FindAsync(report1.Id);
        var r2 = await db.Reports.FindAsync(report2.Id);
        Assert.NotNull(r1?.ConfidenceScore);
        Assert.NotNull(r2?.ConfidenceScore);
        Assert.Equal(0.707f, r1.ConfidenceScore.Value, 3);
        Assert.Equal(0.707f, r2.ConfidenceScore.Value, 3);

        // 3. Login to get token
        var loginRequest = new Winnow.Server.Features.Auth.LoginRequest
        {
            Email = "test@example.com",
            Password = "Password123!"
        };
        var loginResponse = await client.PostAsJsonAsync("/auth/login", loginRequest);
        var cookie = loginResponse.Headers.GetValues("Set-Cookie").FirstOrDefault(c => c.StartsWith("winnow_auth="));
        Assert.NotNull(cookie);
        var token = cookie.Split(';')[0]["winnow_auth=".Length..];

        // 4. Ungroup report 1
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Add("X-Project-ID", _projectId.ToString());

        var response = await client.PostAsJsonAsync($"/reports/{report1.Id}/ungroup", new { });
        Assert.True(response.IsSuccessStatusCode);

        // 5. Verify report 1 is ungrouped
        db.ChangeTracker.Clear();
        var ungroupedReport = await db.Reports.FindAsync(report1.Id);
        Assert.NotNull(ungroupedReport);
        Assert.Null(ungroupedReport.ClusterId);
        Assert.Null(ungroupedReport.ConfidenceScore);
        Assert.Equal("New", ungroupedReport.Status);

        // 6. Verify cluster centroid is updated to report 2's embedding: [0.0, 1.0]
        var finalCluster = await db.Clusters.FindAsync(cluster.Id);
        Assert.NotNull(finalCluster);
        Assert.NotNull(finalCluster.Centroid);
        Assert.Equal(0.0f, finalCluster.Centroid![0], 3);
        Assert.Equal(1.0f, finalCluster.Centroid![1], 3);

        // Verify remaining report confidence is now 1.0 (it IS the centroid)
        var remainingReport = await db.Reports.FindAsync(report2.Id);
        Assert.Equal(1.0f, remainingReport!.ConfidenceScore!.Value, 3);
    }

    [Fact]
    public async Task ReportCreated_UpdatesCentroid()
    {
        using var scope = _app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WinnowDbContext>();

        // 1. Create a cluster with one report
        var cluster = new Cluster
        {
            Id = Guid.NewGuid(),
            ProjectId = _projectId,
            OrganizationId = _organizationId,
            Title = "Test Cluster",
            Status = "Open",
            Centroid = [1.0f, 0.0f]
        };
        db.Clusters.Add(cluster);

        var report1 = new Report
        {
            Id = Guid.NewGuid(),
            ProjectId = _projectId,
            OrganizationId = _organizationId,
            Title = "First Report",
            Message = "Message 1",
            ClusterId = cluster.Id,
            Embedding = [1.0f, 0.0f],
            CreatedAt = DateTime.UtcNow,
            Status = "Grouped"
        };
        db.Reports.Add(report1);
        await db.SaveChangesAsync();

        // 2. Simulate report creation through the consumer
        var clusterService = scope.ServiceProvider.GetRequiredService<IClusterService>();

        var report2 = new Report
        {
            Id = Guid.NewGuid(),
            ProjectId = _projectId,
            OrganizationId = _organizationId,
            Title = "Second Report",
            Message = "Message 2",
            ClusterId = cluster.Id,
            Embedding = [0.0f, 1.0f],
            CreatedAt = DateTime.UtcNow,
            Status = "Grouped"
        };
        db.Reports.Add(report2);
        await db.SaveChangesAsync();

        // Trigger recalculation (simulating what ReportCreatedConsumer does)
        await clusterService.RecalculateCentroidAsync(cluster.Id);
        await db.SaveChangesAsync();

        // 3. Verify centroid is [0.5, 0.5]
        var finalCluster = await db.Clusters.FindAsync(cluster.Id);
        Assert.NotNull(finalCluster);
        Assert.NotNull(finalCluster.Centroid);
        Assert.Equal(0.5f, finalCluster.Centroid![0], 3);
        Assert.Equal(0.5f, finalCluster.Centroid![1], 3);
    }
}
