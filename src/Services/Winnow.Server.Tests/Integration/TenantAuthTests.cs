using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Winnow.Server.Entities;
using Winnow.Server.Features.Auth;
using Winnow.Server.Features.Dashboard;

namespace Winnow.Server.Tests.Integration;

public class TenantAuthTests : IAsyncLifetime
{
    private readonly WinnowTestApp _app;
    private readonly HttpClient _client;
    private const string TestEmail = "auth-test@example.com";
    private const string TestPassword = "Password123!";
    private const string TestTenantId = "test-tenant";

    public TenantAuthTests()
    {
        _app = new WinnowTestApp();
        _client = _app.CreateClient();
    }

    public async Task InitializeAsync()
    {
        using var scope = _app.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var db = scope.ServiceProvider.GetRequiredService<Winnow.Server.Infrastructure.Persistence.WinnowDbContext>();

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

        // Create a project for this user so they can login (LoginEndpoint extracts default project)
        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = "Auth Test Project",
            OwnerId = user.Id,
            ApiKey = "wm_live_auth_test_key"
        };
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
        var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(result);
        Assert.NotEmpty(result.Token);

        // Decode token to verify tenant_id claim
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(result.Token);
        var tenantClaim = token.Claims.FirstOrDefault(c => c.Type == "tenant_id");

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
        var authResult = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        var token = authResult!.Token;
        var projectId = authResult.DefaultProjectId;

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
