using System.Net.Http.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Winnow.API.Domain.Common;
using Winnow.API.Domain.Organizations;
using Winnow.API.Domain.Organizations.ValueObjects;
using Winnow.API.Domain.Projects;
using Winnow.API.Features.Reports.Create;
using Winnow.API.Infrastructure.Identity;
using Winnow.API.Infrastructure.Persistence;
using Winnow.API.Services.Ai;
using Winnow.API.Services.Ai.Strategies;
using Winnow.API.Services.Storage;
using Xunit;

namespace Winnow.API.Tests.Integration;

[Collection("PostgresCollection")]
public class ReportTests : IAsyncLifetime
{
    private readonly WinnowTestApp _app;
    private HttpClient _client = default!;
    private readonly Mock<IEmbeddingService> _embeddingServiceMock;
    private readonly Mock<IStorageService> _storageServiceMock;
    private Guid _projectId;
    private string _apiKey = default!;
    private string? _projectIdHeader;

    public ReportTests(PostgresFixture fixture)
    {
        _embeddingServiceMock = new Mock<IEmbeddingService>();
        _storageServiceMock = new Mock<IStorageService>();

        // Configure mocks
        _embeddingServiceMock
            .Setup(x => x.GetEmbeddingAsync(It.IsAny<string>()))
            .ReturnsAsync(() => new EmbeddingResult(Enumerable.Range(0, 384).Select(i => (float)i / 384).ToArray()));

        _storageServiceMock
            .Setup(x => x.UploadFileAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("test-s3-key");

        // Pass mock services through the configureTestServices parameter
        _app = new WinnowTestApp(fixture, services =>
        {
            // Replace IEmbeddingService with mock
            var embeddingDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IEmbeddingService));
            if (embeddingDescriptor != null) services.Remove(embeddingDescriptor);
            services.AddSingleton(_embeddingServiceMock.Object);

            // Replace IStorageService with mock
            var storageDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IStorageService));
            if (storageDescriptor != null) services.Remove(storageDescriptor);
            services.AddSingleton(_storageServiceMock.Object);
        });
    }

    public async Task InitializeAsync()
    {
        await _app.ResetDatabaseAsync();
        _client = _app.CreateClient();
        // Create a test project in the database
        using var scope = _app.Services.CreateScope();
        var apiKeyService = scope.ServiceProvider.GetRequiredService<Winnow.API.Infrastructure.Security.IApiKeyService>();

        // Generate a proper API key with the correct format: wm_live_{ProjectId}_{RandomSecret}
        _projectId = Guid.NewGuid();
        _apiKey = apiKeyService.GeneratePlaintextKey(_projectId);

        // Create the project with the hashed API key
        using var db = scope.ServiceProvider.GetRequiredService<WinnowDbContext>();
        await db.Database.EnsureCreatedAsync();

        // Create a dummy user for the project owner
        var testUserId = Guid.NewGuid().ToString();
        var testUser = new ApplicationUser
        {
            Id = testUserId,
            UserName = "test@example.com",
            Email = "test@example.com",
            FullName = "Test User",
            CreatedAt = DateTime.UtcNow
        };
        db.Users.Add(testUser);
        await db.SaveChangesAsync();

        // Create a test organization
        var organization = new Organization(
            "Test Organization",
            new Email("test@example.com"),
            SubscriptionPlan.Free);
        var organizationId = organization.Id;
        db.Organizations.Add(organization);

        var ownerRole = await db.Roles.FirstOrDefaultAsync(r => r.Name == "Owner" && r.OrganizationId == null);
        if (ownerRole == null)
        {
            ownerRole = new Winnow.API.Domain.Security.Role("Owner");
            db.Roles.Add(ownerRole);
            await db.SaveChangesAsync();
        }

        // Add user as member of organization
        var organizationMember = new OrganizationMember(
            organizationId,
            testUserId,
            ownerRole.Id);
        db.OrganizationMembers.Add(organizationMember);

        // Create the project with the hashed API key
        var project = new Project(
            organizationId,
            "Test Project",
            testUserId,
            apiKeyService.HashKey(_apiKey),
            _projectId);
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        _projectIdHeader = _projectId.ToString();
    }

    public async Task DisposeAsync()
    {
        await _app.ResetDatabaseAsync();
        _client.Dispose();
        _app.Dispose();
    }

    [Fact]
    public async Task Create_ValidReport_ReturnsSuccess()
    {
        // Arrange
        var request = new IngestReportRequest
        {
            Title = "Test Report Title",
            Message = "This is a test error message for integration testing.",
            StackTrace = "System.NullReferenceException: Object reference not set to an instance of an object.",
            Metadata = new Dictionary<string, object>
            {
                { "environment", "test" },
                { "version", "1.0.0" }
            }
        };

        // Add the required auth headers for API key authentication
        _client.DefaultRequestHeaders.Add("X-Winnow-Key", _apiKey);
        _client.DefaultRequestHeaders.Add("X-Project-ID", _projectIdHeader!);
        _client.DefaultRequestHeaders.Add("X-Tenant-ID", "test-tenant");

        // Act
        var response = await _client.PostAsJsonAsync("/reports", request);

        // Assert
        Assert.True(response.IsSuccessStatusCode,
            $"Expected success status code but got {response.StatusCode}. Response: {await response.Content.ReadAsStringAsync()}");

        // Verify the response contains a GUID
        var result = await response.Content.ReadFromJsonAsync<IngestReportResponse>();
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);

        // Verify embedding service was called
        _embeddingServiceMock.Verify(
            x => x.GetEmbeddingAsync(It.Is<string>(s => s.Contains("Test Report Title"))),
            Times.Once);

        // Verify the report was created in the database
        using var scope = _app.Services.CreateScope();
        using var db = scope.ServiceProvider.GetRequiredService<Winnow.API.Infrastructure.Persistence.WinnowDbContext>();
        var report = await db.Reports.FindAsync(result.Id);
        Assert.NotNull(report);
        Assert.Equal(request.Title, report.Title);
        Assert.Equal(request.Message, report.Message);
        Assert.Equal(request.StackTrace, report.StackTrace);
        Assert.NotNull(report.Embedding);
        Assert.True(report.Embedding.Length > 0);
    }

    [Fact]
    public async Task Create_ReportWithoutApiKey_ReturnsUnauthorized()
    {
        // Arrange
        var request = new IngestReportRequest
        {
            Title = "Test Report",
            Message = "Test message"
        };

        // Do NOT add auth headers

        // Act
        var response = await _client.PostAsJsonAsync("/reports", request);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_ReportWithInvalidApiKey_ReturnsUnauthorized()
    {
        // Arrange
        var request = new IngestReportRequest
        {
            Title = "Test Report",
            Message = "Test message"
        };

        // Add invalid API key
        _client.DefaultRequestHeaders.Add("X-Winnow-Key", "invalid-api-key");
        _client.DefaultRequestHeaders.Add("X-Project-ID", _projectIdHeader!);
        _client.DefaultRequestHeaders.Add("X-Tenant-ID", "test-tenant");

        // Act
        var response = await _client.PostAsJsonAsync("/reports", request);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
