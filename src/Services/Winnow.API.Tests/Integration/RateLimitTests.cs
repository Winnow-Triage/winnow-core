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
public class RateLimitTests(PostgresFixture fixture) : IAsyncLifetime
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

    public async Task InitializeAsync()
    {
        await _app.ResetDatabaseAsync();
        _client = _app.CreateClient();
        var result = await _app.CreateTestProjectAsync();
        _apiKey = result.ApiKey;
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _app.DisposeAsync();
    }

    [Fact]
    public async Task Webhook_RateLimit_PerIP_Triggered()
    {
        // Arrange
        var request = new IngestReportRequest { Title = "Test", Message = "Test" };
        var ip1 = "1.2.3.4";
        var ip2 = "5.6.7.8";

        // Act & Assert - IP 1 hits the limit (5 requests allowed, 6th should fail)
        for (int i = 0; i < 5; i++)
        {
            var response = await SendRequestWithIp(request, ip1);
            Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        }

        var rateLimitedResponse = await SendRequestWithIp(request, ip1);
        Assert.Equal(HttpStatusCode.TooManyRequests, rateLimitedResponse.StatusCode);

        // Act & Assert - IP 2 should still be allowed (Verify Partitioning)
        var responseIp2 = await SendRequestWithIp(request, ip2);
        Assert.Equal(HttpStatusCode.Accepted, responseIp2.StatusCode);
    }

    private async Task<HttpResponseMessage> SendRequestWithIp(IngestReportRequest request, string ip)
    {
        var timestamp = DateTimeOffset.UtcNow.ToString("O");
        var nonce = MineNonce(_apiKey, "POST", "/reports", timestamp, 4);

        var message = new HttpRequestMessage(HttpMethod.Post, "/reports")
        {
            Content = JsonContent.Create(request)
        };

        message.Headers.Add("X-Winnow-Key", _apiKey);
        message.Headers.Add("X-Winnow-PoW-Nonce", nonce);
        message.Headers.Add("X-Winnow-PoW-Timestamp", timestamp);
        message.Headers.Add("X-Forwarded-For", ip);

        return await _client.SendAsync(message);
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
            if (nonce > 1000000) throw new Exception("Failed to mine nonce");
        }
    }
}
