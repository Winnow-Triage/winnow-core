using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Winnow.Server.Features.Reports.Create;
using Winnow.Server.Services.Ai;
using Winnow.Server.Services.Storage;

namespace Winnow.Server.Tests.Integration;

public class ReportTests : IAsyncLifetime
{
    private readonly WinnowTestApp _app;
    private readonly HttpClient _client;
    private readonly Mock<IEmbeddingService> _embeddingServiceMock;
    private readonly Mock<IStorageService> _storageServiceMock;
    private Guid _projectId;
    private const string TestApiKey = "test-api-key-123";

    public ReportTests()
    {
        _embeddingServiceMock = new Mock<IEmbeddingService>();
        _storageServiceMock = new Mock<IStorageService>();

        // Configure mocks
        _embeddingServiceMock
            .Setup(x => x.GetEmbeddingAsync(It.IsAny<string>()))
            .ReturnsAsync(() => [.. Enumerable.Range(0, 384).Select(i => (float)i / 384)]);

        _storageServiceMock
            .Setup(x => x.UploadFileAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("test-s3-key");

        // Create the test application with mocked services using constructor
        _app = new WinnowTestApp(services =>
        {
            // Replace IEmbeddingService with mock
            var embeddingDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IEmbeddingService));
            if (embeddingDescriptor != null)
            {
                services.Remove(embeddingDescriptor);
            }
            services.AddSingleton(_embeddingServiceMock.Object);

            // Replace IStorageService with mock
            var storageDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IStorageService));
            if (storageDescriptor != null)
            {
                services.Remove(storageDescriptor);
            }
            services.AddSingleton(_storageServiceMock.Object);
        });

        _client = _app.CreateClient();
    }

    public async Task InitializeAsync()
    {
        // Create a test project in the database
        _projectId = await _app.CreateTestProjectAsync(TestApiKey);
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

        // Add the required API key header
        _client.DefaultRequestHeaders.Add("X-Winnow-Key", TestApiKey);

        // Act
        var response = await _client.PostAsJsonAsync("/api/reports", request);

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
        using var db = scope.ServiceProvider.GetRequiredService<Winnow.Server.Infrastructure.Persistence.WinnowDbContext>();
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

        // Do NOT add the API key header

        // Act
        var response = await _client.PostAsJsonAsync("/api/reports", request);

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

        // Act
        var response = await _client.PostAsJsonAsync("/api/reports", request);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }
}