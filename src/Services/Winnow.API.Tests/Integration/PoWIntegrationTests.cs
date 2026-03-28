using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Winnow.API.Features.Reports.Create;
using Winnow.API.Services.Ai;
using Winnow.API.Services.Ai.Strategies;
using Xunit;

namespace Winnow.API.Tests.Integration;

[Collection("PostgresCollection")]
public class PoWIntegrationTests(PostgresFixture fixture) : IAsyncLifetime
{
    private readonly WinnowTestApp _app = new(fixture, services =>
        {
            // Mock AI service to avoid overhead
            var embeddingDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IEmbeddingService));
            if (embeddingDescriptor != null) services.Remove(embeddingDescriptor);

            var mock = new Mock<IEmbeddingService>();
            mock.Setup(x => x.GetEmbeddingAsync(It.IsAny<string>()))
                .ReturnsAsync(new EmbeddingResult(new float[384]));
            services.AddSingleton(mock.Object);
        }, enablePoW: true);
    private HttpClient _client = default!;
    private string _apiKey = default!;
    private Guid _projectId;

    public async Task InitializeAsync()
    {
        await _app.ResetDatabaseAsync();
        _client = _app.CreateClient();

        // Setup a project and API key
        var result = await _app.CreateTestProjectAsync();
        _projectId = result.ProjectId;
        _apiKey = result.ApiKey;
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _app.DisposeAsync();
    }

    [Fact]
    public async Task IngestReport_WithValidPoW_ReturnsAccepted()
    {
        // Arrange
        var request = new IngestReportRequest { Title = "Test", Message = "Test" };
        var timestamp = DateTimeOffset.UtcNow.ToString("O");
        var nonce = MineNonce(_apiKey, "POST", "/reports", timestamp, 4);

        _client.DefaultRequestHeaders.Add("X-Winnow-Key", _apiKey);
        _client.DefaultRequestHeaders.Add("X-Winnow-PoW-Nonce", nonce);
        _client.DefaultRequestHeaders.Add("X-Winnow-PoW-Timestamp", timestamp);

        // Act
        var response = await _client.PostAsJsonAsync("/reports", request);

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }

    [Fact]
    public async Task IngestReport_MissingPoWHeaders_ReturnsBadRequest()
    {
        // Arrange
        var request = new IngestReportRequest { Title = "Test", Message = "Test" };
        _client.DefaultRequestHeaders.Add("X-Winnow-Key", _apiKey);

        // Act
        var response = await _client.PostAsJsonAsync("/reports", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Missing Proof-of-Work headers", body);
    }

    [Fact]
    public async Task IngestReport_InvalidPoWNonce_ReturnsBadRequest()
    {
        // Arrange
        var request = new IngestReportRequest { Title = "Test", Message = "Test" };
        var timestamp = DateTimeOffset.UtcNow.ToString("O");

        _client.DefaultRequestHeaders.Add("X-Winnow-Key", _apiKey);
        _client.DefaultRequestHeaders.Add("X-Winnow-PoW-Nonce", "invalid-nonce");
        _client.DefaultRequestHeaders.Add("X-Winnow-PoW-Timestamp", timestamp);

        // Act
        var response = await _client.PostAsJsonAsync("/reports", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Invalid Proof-of-Work solution", body);
    }

    [Fact]
    public async Task IngestReport_ExpiredTimestamp_ReturnsBadRequest()
    {
        // Arrange
        var request = new IngestReportRequest { Title = "Test", Message = "Test" };
        // 10 minutes ago (default limit is 5)
        var timestampStr = DateTimeOffset.UtcNow.AddMinutes(-10).ToString("O");
        var nonce = MineNonce(_apiKey, "POST", "/reports", timestampStr, 4);

        _client.DefaultRequestHeaders.Add("X-Winnow-Key", _apiKey);
        _client.DefaultRequestHeaders.Add("X-Winnow-PoW-Nonce", nonce);
        _client.DefaultRequestHeaders.Add("X-Winnow-PoW-Timestamp", timestampStr);

        // Act
        var response = await _client.PostAsJsonAsync("/reports", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Proof-of-Work timestamp is expired", body);
    }

    [Fact]
    public async Task IngestReport_ReusedNonce_ReturnsBadRequest()
    {
        // Arrange
        var request = new IngestReportRequest { Title = "Test", Message = "Test" };
        var timestamp = DateTimeOffset.UtcNow.ToString("O");
        var nonce = MineNonce(_apiKey, "POST", "/reports", timestamp, 4);

        _client.DefaultRequestHeaders.Add("X-Winnow-Key", _apiKey);
        _client.DefaultRequestHeaders.Add("X-Winnow-PoW-Nonce", nonce);
        _client.DefaultRequestHeaders.Add("X-Winnow-PoW-Timestamp", timestamp);

        // Act - First request (success)
        var response1 = await _client.PostAsJsonAsync("/reports", request);
        Assert.Equal(HttpStatusCode.Accepted, response1.StatusCode);

        // Act - Second request (replay)
        var response2 = await _client.PostAsJsonAsync("/reports", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response2.StatusCode);
        var body = await response2.Content.ReadAsStringAsync();
        Assert.Contains("Proof-of-Work nonce has already been used", body);
    }

    private string MineNonce(string apiKey, string method, string path, string timestamp, int difficulty)
    {
        var target = new string('0', difficulty);
        int nonce = 0;
        while (true)
        {
            var nonceStr = nonce.ToString();
            var data = $"{apiKey}{method.ToUpperInvariant()}{path.ToLowerInvariant()}{timestamp}{nonceStr}";
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(data))).ToLowerInvariant();
            if (hash.StartsWith(target)) return nonceStr;
            nonce++;
            if (nonce > 1000000) throw new Exception("Failed to mine nonce in reasonable time");
        }
    }
}
