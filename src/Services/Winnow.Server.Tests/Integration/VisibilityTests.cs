using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Winnow.Server.Entities;
using Winnow.Server.Features.Auth;
using Winnow.Server.Features.Projects;
using Winnow.Server.Features.Clusters.List;
using Xunit;

namespace Winnow.Server.Tests.Integration;

[Collection("PostgresCollection")]
public class VisibilityTests : IAsyncLifetime
{
    private readonly WinnowTestApp _app;
    private HttpClient _client = default!;
    private string _adminToken = default!;
    private string _memberToken = default!;
    private Guid _orgId;
    private Guid _projectId;

    public VisibilityTests(PostgresFixture fixture)
    {
        _app = new WinnowTestApp(fixture);
    }

    public async Task InitializeAsync()
    {
        await _app.ResetDatabaseAsync();
        _client = _app.CreateClient();

        using var scope = _app.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var db = scope.ServiceProvider.GetRequiredService<Winnow.Server.Infrastructure.Persistence.WinnowDbContext>();

        // 1. Setup Organization
        _orgId = Guid.NewGuid();
        var org = new Organization { Id = _orgId, Name = "Visibility Org", SubscriptionTier = "pro" };
        db.Organizations.Add(org);

        // 2. Setup Admin User
        var adminUser = new ApplicationUser { UserName = "admin@test.com", Email = "admin@test.com", FullName = "Admin User" };
        await userManager.CreateAsync(adminUser, "Password123!");
        db.OrganizationMembers.Add(new OrganizationMember { UserId = adminUser.Id, OrganizationId = _orgId, Role = "Admin" });

        // 3. Setup Member User
        var memberUser = new ApplicationUser { UserName = "member@test.com", Email = "member@test.com", FullName = "Member User" };
        await userManager.CreateAsync(memberUser, "Password123!");
        db.OrganizationMembers.Add(new OrganizationMember { UserId = memberUser.Id, OrganizationId = _orgId, Role = "Member" });

        // 4. Setup Project (owned by Admin)
        _projectId = Guid.NewGuid();
        var project = new Project(
            _orgId,
            "Test Project",
            adminUser.Id,
            "wm_live_test_key",
            _projectId);
        db.Projects.Add(project);

        // 5. Add a Cluster to the Project
        var cluster = new Cluster
        {
            Id = Guid.NewGuid(),
            ProjectId = _projectId,
            Title = "Test Cluster",
            Status = ClusterStatus.Active
        };
        db.Clusters.Add(cluster);

        await db.SaveChangesAsync();

        // 6. Get Tokens
        _adminToken = await GetTokenAsync("admin@test.com", "Password123!");
        _memberToken = await GetTokenAsync("member@test.com", "Password123!");
    }

    private async Task<string> GetTokenAsync(string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/auth/login", new LoginRequest { Email = email, Password = password });
        var cookie = response.Headers.GetValues("Set-Cookie").First(c => c.StartsWith("winnow_auth="));
        return cookie.Split(';')[0].Substring("winnow_auth=".Length);
    }

    [Fact]
    public async Task Admin_CanSeeAllProjects()
    {
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _adminToken);
        var response = await _client.GetAsync("/projects");

        Assert.True(response.IsSuccessStatusCode);
        var projects = await response.Content.ReadFromJsonAsync<List<ProjectDto>>();
        Assert.Contains(projects!, p => p.Id == _projectId);
    }

    [Fact]
    public async Task RegularMember_CannotSeeProject_IfNotMember()
    {
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _memberToken);
        var response = await _client.GetAsync("/projects");

        Assert.True(response.IsSuccessStatusCode);
        var projects = await response.Content.ReadFromJsonAsync<List<ProjectDto>>();
        Assert.DoesNotContain(projects!, p => p.Id == _projectId);
    }

    [Fact]
    public async Task Admin_CanSeeClusters_InAnyProject()
    {
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _adminToken);
        _client.DefaultRequestHeaders.Add("X-Project-ID", _projectId.ToString());

        var response = await _client.GetAsync("/clusters");

        Assert.True(response.IsSuccessStatusCode);
        var clusters = await response.Content.ReadFromJsonAsync<List<ClusterDto>>();
        Assert.NotEmpty(clusters!);
    }

    [Fact]
    public async Task Member_CanSeeClusters_IfAddedAsProjectMember()
    {
        // Arrange: Add member to project
        using (var scope = _app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<Winnow.Server.Infrastructure.Persistence.WinnowDbContext>();
            var user = await db.Users.FirstAsync(u => u.Email == "member@test.com");
            db.ProjectMembers.Add(new ProjectMember { ProjectId = _projectId, UserId = user.Id });
            await db.SaveChangesAsync();
        }

        // Act
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _memberToken);
        _client.DefaultRequestHeaders.Add("X-Project-ID", _projectId.ToString());

        var response = await _client.GetAsync("/clusters");

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        var clusters = await response.Content.ReadFromJsonAsync<List<ClusterDto>>();
        Assert.NotEmpty(clusters!);
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _app.ResetDatabaseAsync();
    }
}
