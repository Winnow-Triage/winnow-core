using Winnow.Server.Features.Auth.Login;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Winnow.Server.Domain.Clusters;
using Winnow.Server.Domain.Clusters.ValueObjects;
using Winnow.Server.Domain.Common;
using Winnow.Server.Domain.Organizations;
using Winnow.Server.Domain.Organizations.ValueObjects;
using Winnow.Server.Domain.Projects;
using Winnow.Server.Domain.Reports;
using Winnow.Server.Domain.Reports.ValueObjects;
using Winnow.Server.Infrastructure.Identity;
using Winnow.Server.Infrastructure.Persistence;
using Winnow.Server.Services.Ai;
using Xunit;

namespace Winnow.Server.Tests.Integration;

[Collection("PostgresCollection")]
public class CentroidRecalculationTests(PostgresFixture fixture) : IAsyncLifetime
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

        var orgMember = new OrganizationMember(_organizationId, userId, "owner");
        db.OrganizationMembers.Add(orgMember);

        var project = new Project(_organizationId, "Test Project", userId, "test-hash");
        _projectId = project.Id;
        db.Projects.Add(project);

        await db.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task UngroupReport_UpdatesCentroid()
    {
        var client = _app.CreateClient();
        using var scope = _app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WinnowDbContext>();

        // 1. Create reports
        var report1 = new Report(_projectId, _organizationId, "Report 1", "Message 1");
        report1.SetEmbedding(MakeVector(1.0f, 0.0f));
        report1.ChangeStatus(ReportStatus.Duplicate);

        var report2 = new Report(_projectId, _organizationId, "Report 2", "Message 2");
        report2.SetEmbedding(MakeVector(0.0f, 1.0f));
        report2.ChangeStatus(ReportStatus.Duplicate);

        db.Reports.AddRange(report1, report2);
        await db.SaveChangesAsync();

        // 2. Create a cluster with these reports
        var cluster = new Cluster(_projectId, _organizationId, report1.Id);
        cluster.AddReport(report2.Id);
        cluster.UpdateCentroid(MakeVector(1.0f, 0.0f)); // Initial dummy centroid
        db.Clusters.Add(cluster);
        await db.SaveChangesAsync();

        // Assign reports to cluster in DB
        report1.AssignToCluster(cluster.Id);
        report2.AssignToCluster(cluster.Id);
        await db.SaveChangesAsync();

        // 3. Initial centroid should be average: [0.5, 0.5]
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
        Assert.Equal(0.707f, r1.ConfidenceScore.Value.Score, 3);
        Assert.Equal(0.707f, r2.ConfidenceScore.Value.Score, 3);

        // 4. Login to get token
        var loginRequest = new Winnow.Server.Features.Auth.Login.LoginRequest
        {
            Email = "test@example.com",
            Password = "Password123!"
        };
        var loginResponse = await client.PostAsJsonAsync("/auth/login", loginRequest);
        var cookie = loginResponse.Headers.GetValues("Set-Cookie").FirstOrDefault(c => c.StartsWith("winnow_auth="));
        Assert.NotNull(cookie);
        var token = cookie.Split(';')[0]["winnow_auth=".Length..];

        // 5. Ungroup report 1
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Add("X-Project-ID", _projectId.ToString());

        var response = await client.PostAsJsonAsync($"/reports/{report1.Id}/ungroup", new { });
        Assert.True(response.IsSuccessStatusCode);

        // 6. Verify report 1 is ungrouped
        db.ChangeTracker.Clear();
        var ungroupedReport = await db.Reports.FindAsync(report1.Id);
        Assert.NotNull(ungroupedReport);
        Assert.Null(ungroupedReport.ClusterId);
        Assert.Null(ungroupedReport.ConfidenceScore);
        Assert.Equal(ReportStatus.Open, ungroupedReport.Status);

        // 7. Verify cluster centroid is updated to report 2's embedding: [0.0, 1.0]
        var finalCluster = await db.Clusters.FindAsync(cluster.Id);
        Assert.NotNull(finalCluster);
        Assert.NotNull(finalCluster.Centroid);
        Assert.Equal(0.0f, finalCluster.Centroid![0], 3);
        Assert.Equal(1.0f, finalCluster.Centroid![1], 3);

        // Verify remaining report confidence is now 1.0 (it IS the centroid)
        var remainingReport = await db.Reports.FindAsync(report2.Id);
        Assert.Equal(1.0f, remainingReport!.ConfidenceScore!.Value.Score, 3);
    }

    [Fact]
    public async Task ReportCreated_UpdatesCentroid()
    {
        using var scope = _app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WinnowDbContext>();

        // 1. Create a report and its cluster
        var report1 = new Report(_projectId, _organizationId, "First Report", "Message 1");
        report1.SetEmbedding(MakeVector(1.0f, 0.0f));
        report1.ChangeStatus(ReportStatus.Duplicate);
        db.Reports.Add(report1);
        await db.SaveChangesAsync();

        var cluster = new Cluster(_projectId, _organizationId, report1.Id);
        cluster.UpdateCentroid(MakeVector(1.0f, 0.0f));
        db.Clusters.Add(cluster);
        await db.SaveChangesAsync();

        report1.AssignToCluster(cluster.Id);
        await db.SaveChangesAsync();

        // 2. Simulate report creation through the consumer
        var clusterService = scope.ServiceProvider.GetRequiredService<IClusterService>();

        var report2 = new Report(_projectId, _organizationId, "Second Report", "Message 2");
        report2.SetEmbedding(MakeVector(0.0f, 1.0f));
        report2.ChangeStatus(ReportStatus.Duplicate);
        db.Reports.Add(report2);
        await db.SaveChangesAsync();

        // Manually assign to cluster (simulating clustering logic)
        report2.AssignToCluster(cluster.Id);
        cluster.AddReport(report2.Id);
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
