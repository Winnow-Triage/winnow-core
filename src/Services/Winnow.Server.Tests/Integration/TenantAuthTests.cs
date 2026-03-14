using Winnow.Server.Features.Dashboard.Dtos;
using Winnow.Server.Features.Auth.Auth;
using Winnow.Server.Features.Auth.Login;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Winnow.Server.Domain.Common;
using Winnow.Server.Domain.Organizations;
using Winnow.Server.Domain.Projects;
using Winnow.Server.Features.Auth;
using Winnow.Server.Features.Dashboard;
using Winnow.Server.Infrastructure.Identity;
using Winnow.Server.Infrastructure.Persistence;
using Xunit;

namespace Winnow.Server.Tests.Integration;

[Collection("PostgresCollection")]
public class TenantAuthTests : IAsyncLifetime
{
    private readonly WinnowTestApp _app;
    private HttpClient _client = default!;
    private const string TestEmail = "auth-test@example.com";
    private const string TestPassword = "Password123!";
    private const string TestTenantId = "test-tenant";

    public TenantAuthTests(PostgresFixture fixture)
    {
        _app = new WinnowTestApp(fixture);
    }

    public async Task InitializeAsync()
    {
        await _app.ResetDatabaseAsync();
        _client = _app.CreateClient();
        using var scope = _app.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var db = scope.ServiceProvider.GetRequiredService<WinnowDbContext>();

        // Ensure DB is created
        await db.Database.EnsureCreatedAsync();

        // Create a user with a known password
        var user = new ApplicationUser
        {
            UserName = TestEmail,
            Email = TestEmail,
            FullName = "Auth Test User"
        };

        var result = await userManager.CreateAsync(user, TestPassword);
        if (!result.Succeeded)
        {
            throw new Exception($"Failed to create test user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        // Check if default organization already exists (from migration)
        var defaultOrganizationId = new Guid("11111111-1111-1111-1111-111111111111");
        var defaultOrganization = await db.Organizations.FindAsync(defaultOrganizationId);

        if (defaultOrganization == null)
        {
            // Note: In rich model, we can't easily force ID via constructor if not supported, 
            // but Project supports it. Organization doesn't show ID in ctor in my view.
            // I'll check Organization.cs again if it has 'Id = id' in ctor.
            // Wait, I saw 'Id = Guid.NewGuid()' in Organization.cs ctor.
            // I'll just use the ctor and if I really need that ID I'll use reflection or just change the test to not rely on hardcoded Guid.
            defaultOrganization = new Organization("Default Organization", new Email("admin@example.com"));
            db.Organizations.Add(defaultOrganization);
            await db.SaveChangesAsync(); // To get the ID generated
        }

        // Check if user is already an organization member
        var existingMember = await db.OrganizationMembers
            .FirstOrDefaultAsync(m => m.UserId == user.Id && m.OrganizationId == defaultOrganization.Id);

        if (existingMember == null)
        {
            var orgMember = new OrganizationMember(defaultOrganization.Id, user.Id, "admin");
            db.OrganizationMembers.Add(orgMember);
        }

        // Create a project for this user so they can login (LoginEndpoint extracts default project)
        var project = new Project(defaultOrganization.Id, "Auth Test Project", user.Id, "wm_live_auth_test_key");
        db.Projects.Add(project);
        await db.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await _app.ResetDatabaseAsync();
        _client.Dispose();
        _app.Dispose();
    }

    [Fact]
    public async Task Login_WithTenantHeader_ReturnsTokenWithTenantClaim()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = TestEmail,
            Password = TestPassword
        };
        _client.DefaultRequestHeaders.Add("X-Tenant-ID", TestTenantId);

        // Act
        var response = await _client.PostAsJsonAsync("/auth/login", request);

        // Assert
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        var result = await response.Content.ReadFromJsonAsync<AuthResult>();
        Assert.NotNull(result);

        var cookie = response.Headers.GetValues("Set-Cookie").FirstOrDefault(c => c.StartsWith("winnow_auth="));
        Assert.NotNull(cookie);
        var token = cookie.Split(';')[0].Substring("winnow_auth=".Length);
        Assert.NotEmpty(token);

        // Decode token to verify tenant_id claim
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        var tenantClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "tenant_id");

        Assert.NotNull(tenantClaim);
        Assert.Equal(TestTenantId, tenantClaim.Value);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithTokenButNoHeader_Succeeds()
    {
        // 1. Login to get token
        var loginRequest = new LoginRequest { Email = TestEmail, Password = TestPassword };
        _client.DefaultRequestHeaders.Add("X-Tenant-ID", TestTenantId);
        var loginResponse = await _client.PostAsJsonAsync("/auth/login", loginRequest);
        var authResult = await loginResponse.Content.ReadFromJsonAsync<AuthResult>();

        var cookie = loginResponse.Headers.GetValues("Set-Cookie").First(c => c.StartsWith("winnow_auth="));
        var token = cookie.Split(';')[0].Substring("winnow_auth=".Length);

        var projectId = authResult!.DefaultProjectId;

        // 2. Clear headers and set Authorization
        _client.DefaultRequestHeaders.Remove("X-Tenant-ID");
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        _client.DefaultRequestHeaders.Add("X-Project-ID", projectId.ToString()); // Required by Dashboard endpoint logic

        // 3. Call protected endpoint (Dashboard Metrics)
        var response = await _client.GetAsync("/dashboard/metrics");

        // Assert
        Assert.True(response.IsSuccessStatusCode,
            $"Expected success but got {response.StatusCode}. Content: {await response.Content.ReadAsStringAsync()}");

        var metrics = await response.Content.ReadFromJsonAsync<DashboardMetricsDto>();
        Assert.NotNull(metrics);
    }
}
