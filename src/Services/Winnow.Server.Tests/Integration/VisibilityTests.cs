using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Winnow.Server.Domain.Clusters;
using Winnow.Server.Domain.Common;
using Winnow.Server.Domain.Organizations;
using Winnow.Server.Domain.Projects;
using Winnow.Server.Features.Auth;
using Winnow.Server.Features.Clusters.List;
using Winnow.Server.Features.Projects;
using Winnow.Server.Infrastructure.Identity;
using Winnow.Server.Infrastructure.Persistence;
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
        _client = _app.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            HandleCookies = false
        });

        using var scope = _app.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var db = scope.ServiceProvider.GetRequiredService<WinnowDbContext>();

        // 1. Setup Organization
        var org = new Organization("Visibility Org", new Email("admin@test.com"));
        _orgId = org.Id;
        db.Organizations.Add(org);

        // 2. Setup Admin User
        var adminUser = new ApplicationUser { UserName = "admin@test.com", Email = "admin@test.com", FullName = "Admin User" };
        await userManager.CreateAsync(adminUser, "Password123!");
        db.OrganizationMembers.Add(new OrganizationMember(_orgId, adminUser.Id, "Admin"));

        // 3. Setup Member User
        var memberUser = new ApplicationUser { UserName = "member@test.com", Email = "member@test.com", FullName = "Member User" };
        await userManager.CreateAsync(memberUser, "Password123!");
        db.OrganizationMembers.Add(new OrganizationMember(_orgId, memberUser.Id, "Member"));

        // 4. Setup Team and Project (owned by Admin)
        var team = new Winnow.Server.Domain.Teams.Team(_orgId, "Test Team");
        db.Teams.Add(team);

        _projectId = Guid.NewGuid();
        var project = new Project(
            _orgId,
            "Test Project",
            adminUser.Id,
            "wm_live_test_key",
            _projectId);

        project.ChangeTeam(team.Id);
        db.Projects.Add(project);

        // 5. Add a Cluster to the Project
        var cluster = new Cluster(_projectId, _orgId, Guid.NewGuid());
        db.Clusters.Add(cluster);

        await db.SaveChangesAsync();

        // 6. Get Tokens
        _adminToken = await GetTokenAsync("admin@test.com", "Password123!");
        _memberToken = await GetTokenAsync("member@test.com", "Password123!");
    }

    private async Task<string> GetTokenAsync(string email, string password)
    {
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("X-Tenant-ID", "test-tenant");
        var response = await _client.PostAsJsonAsync("/auth/login", new LoginRequest { Email = email, Password = password });
        response.EnsureSuccessStatusCode();
        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return authResponse?.Token ?? throw new InvalidOperationException("Token not found in login response.");
    }

    [Fact]
    public async Task Admin_CanSeeAllProjects()
    {
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("X-Tenant-ID", "test-tenant");
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _adminToken);
        var response = await _client.GetAsync("/projects");

        Assert.True(response.IsSuccessStatusCode);
        var projects = await response.Content.ReadFromJsonAsync<List<ProjectDto>>();
        Assert.Contains(projects!, p => p.Id == _projectId);
    }

    [Fact]
    public async Task RegularMember_CannotSeeProject_IfNotMember()
    {
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("X-Tenant-ID", "test-tenant");
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _memberToken);
        var response = await _client.GetAsync("/projects");

        Assert.True(response.IsSuccessStatusCode);
        var projects = await response.Content.ReadFromJsonAsync<List<ProjectDto>>();
        Assert.DoesNotContain(projects!, p => p.Id == _projectId);
    }

    [Fact]
    public async Task Admin_CanSeeClusters_InAnyProject()
    {
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("X-Tenant-ID", "test-tenant");
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
            var db = scope.ServiceProvider.GetRequiredService<WinnowDbContext>();
            var user = await db.Users.FirstAsync(u => u.Email == "member@test.com");
            db.ProjectMembers.Add(new ProjectMember(_projectId, user.Id));
            await db.SaveChangesAsync();
        }

        // Act
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("X-Tenant-ID", "test-tenant");
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
