using Winnow.API.Features.Projects.Dtos;
using Winnow.API.Features.Auth.Auth;
using Winnow.API.Features.Auth.Login;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Winnow.API.Domain.Clusters;
using Winnow.API.Domain.Common;
using Winnow.API.Domain.Organizations;
using Winnow.API.Domain.Projects;
using Winnow.API.Features.Auth;
using Winnow.API.Features.Clusters.List;
using Winnow.API.Features.Projects;
using Winnow.API.Infrastructure.Identity;
using Winnow.API.Infrastructure.Persistence;
using Xunit;

namespace Winnow.API.Tests.Integration;

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

        // 1. Seed global roles and permissions
        await _app.SeedDefaultDataAsync();

        // 2. Setup Organization
        var org = new Organization("Visibility Org", new Email("admin@test.com"));
        _orgId = org.Id;
        db.Organizations.Add(org);

        // 3. Setup Admin User
        var adminRole = await db.Roles.FirstAsync(r => r.Name == "Admin" && r.OrganizationId == null);
        var adminUser = new ApplicationUser { UserName = "admin@test.com", Email = "admin@test.com", FullName = "Admin User" };
        await userManager.CreateAsync(adminUser, "Password123!");
        db.OrganizationMembers.Add(new OrganizationMember(_orgId, adminUser.Id, adminRole.Id));

        // 4. Setup Member User
        var memberRole = await db.Roles.FirstAsync(r => r.Name == "Member" && r.OrganizationId == null);
        var memberUser = new ApplicationUser { UserName = "member@test.com", Email = "member@test.com", FullName = "Member User" };
        await userManager.CreateAsync(memberUser, "Password123!");
        db.OrganizationMembers.Add(new OrganizationMember(_orgId, memberUser.Id, memberRole.Id));

        // 5. Setup Team and Project (owned by Admin)
        var team = new Winnow.API.Domain.Teams.Team(_orgId, "Test Team");
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

        // 6. Add a Cluster to the Project
        var cluster = new Cluster(_projectId, _orgId, Guid.NewGuid());
        db.Clusters.Add(cluster);

        await db.SaveChangesAsync();

        // 7. Get Tokens
        _adminToken = await GetTokenAsync("admin@test.com", "Password123!");
        _memberToken = await GetTokenAsync("member@test.com", "Password123!");
    }

    private async Task<string> GetTokenAsync(string email, string password)
    {
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("X-Tenant-ID", "test-tenant");
        var response = await _client.PostAsJsonAsync("/auth/login", new LoginRequest { Email = email, Password = password });
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Login failed with status {response.StatusCode}: {error}");
        }
        var authResult = await response.Content.ReadFromJsonAsync<AuthResult>();
        return authResult?.Token ?? throw new InvalidOperationException("Token not found in login response.");
    }

    [Fact]
    public async Task Admin_CanSeeAllProjects()
    {
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("X-Tenant-ID", "test-tenant");
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _adminToken);
        var response = await _client.GetAsync("/projects");

        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            Assert.Fail($"Status {response.StatusCode}: {content}");
        }
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

        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            Assert.Fail($"Status {response.StatusCode}: {content}");
        }
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

        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            Assert.Fail($"Status {response.StatusCode}: {content}");
        }
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
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            Assert.Fail($"Status {response.StatusCode}: {content}");
        }
        var clusters = await response.Content.ReadFromJsonAsync<List<ClusterDto>>();
        Assert.NotEmpty(clusters!);
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _app.ResetDatabaseAsync();
    }
}
