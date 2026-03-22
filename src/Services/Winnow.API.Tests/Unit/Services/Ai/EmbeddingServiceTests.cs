using Microsoft.Extensions.Logging;
using Moq;
using Winnow.API.Infrastructure.Configuration;
using Winnow.API.Services.Ai;
using Winnow.API.Services.Ai.Strategies;

namespace Winnow.API.Tests.Unit.Services.Ai;

public class EmbeddingServiceTests
{
    private readonly Mock<ILogger<EmbeddingService>> _loggerMock;
    private readonly LlmSettings _settings;
    private readonly List<Mock<IEmbeddingProvider>> _providerMocks;
    private readonly EmbeddingService _embeddingService;

    public EmbeddingServiceTests()
    {
        _loggerMock = new Mock<ILogger<EmbeddingService>>();
        _settings = new LlmSettings { Provider = "Ollama" };

        // Create mock providers
        _providerMocks =
        [
            new(),
            new(),
            new()
        ];

        var providers = _providerMocks.Select(m => m.Object).ToList();
        _embeddingService = new EmbeddingService(_loggerMock.Object, _settings, providers);
    }

    [Fact]
    public void Constructor_WhenCalled_LogsProviderCount()
    {
        // Arrange & Act is done in constructor

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Initialized with")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)!),
            Times.Once);
    }

    [Fact]
    public async Task GetEmbeddingAsync_WithEmptyText_ThrowsArgumentException()
    {
        // Arrange
        var emptyText = "";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _embeddingService.GetEmbeddingAsync(emptyText));
    }

    [Fact]
    public async Task GetEmbeddingAsync_SelectsProviderThatCanHandleSettings()
    {
        // Arrange
        var text = "test text";
        var expectedEmbedding = new float[384];
        var random = new Random();
        for (int i = 0; i < expectedEmbedding.Length; i++) expectedEmbedding[i] = (float)random.NextDouble();
 
        // First provider cannot handle settings
        _providerMocks[0].Setup(p => p.CanHandle(_settings)).Returns(false);
 
        // Second provider can handle settings
        _providerMocks[1].Setup(p => p.CanHandle(_settings)).Returns(true);
        _providerMocks[1].Setup(p => p.GetEmbeddingAsync(text)).ReturnsAsync(new EmbeddingResult(expectedEmbedding));
 
        // Third provider can also handle but should not be selected
        _providerMocks[2].Setup(p => p.CanHandle(_settings)).Returns(true);
 
        // Act
        var result = await _embeddingService.GetEmbeddingAsync(text);
 
        // Assert
        Assert.Equal(expectedEmbedding, result.Vector);
        _providerMocks[0].Verify(p => p.CanHandle(_settings), Times.Once);
        _providerMocks[1].Verify(p => p.CanHandle(_settings), Times.Once);
        _providerMocks[1].Verify(p => p.GetEmbeddingAsync(text), Times.Once);
        _providerMocks[2].Verify(p => p.CanHandle(_settings), Times.Never); // Should not be checked after finding a provider
    }

    [Fact]
    public async Task GetEmbeddingAsync_WhenNoProviderCanHandleSettings_ThrowsInvalidOperationException()
    {
        // Arrange
        var text = "test text";

        // No provider can handle settings
        foreach (var mock in _providerMocks)
        {
            mock.Setup(p => p.CanHandle(_settings)).Returns(false);
        }

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _embeddingService.GetEmbeddingAsync(text));
    }

    [Fact]
    public async Task GetEmbeddingAsync_WhenProviderFails_ThrowsException()
    {
        // Arrange
        var text = "test text";
        var exception = new Exception("Provider failed");

        // First provider can handle but fails
        _providerMocks[0].Setup(p => p.CanHandle(_settings)).Returns(true);
        _providerMocks[0].Setup(p => p.GetEmbeddingAsync(text)).ThrowsAsync(exception);

        // Act & Assert
        var caughtException = await Assert.ThrowsAsync<Exception>(() => _embeddingService.GetEmbeddingAsync(text));
        Assert.Equal("Provider failed", caughtException.Message);
    }

    [Fact]
    public async Task GetEmbeddingAsync_WhenNoProvidersRegistered_ThrowsInvalidOperationException()
    {
        // Arrange
        var text = "test text";
        var emptyService = new EmbeddingService(_loggerMock.Object, _settings, []);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => emptyService.GetEmbeddingAsync(text));
    }

    [Fact]
    public async Task GetEmbeddingAsync_WithValidTextAndProvider_ReturnsProviderResult()
    {
        // Arrange
        var text = "This is a test text for embedding generation";
        var expectedEmbedding = new float[384];
        var random = new Random();
        for (int i = 0; i < expectedEmbedding.Length; i++) expectedEmbedding[i] = (float)random.NextDouble();
 
        _providerMocks[0].Setup(p => p.CanHandle(_settings)).Returns(true);
        _providerMocks[0].Setup(p => p.GetEmbeddingAsync(text)).ReturnsAsync(new EmbeddingResult(expectedEmbedding));
 
        // Act
        var result = await _embeddingService.GetEmbeddingAsync(text);
 
        // Assert
        Assert.Equal(expectedEmbedding, result.Vector);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Selected provider")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)!),
            Times.Once);
    }

    [Fact]
    public async Task SelectProvider_WithMultipleProviders_SelectsFirstThatCanHandle()
    {
        // This is a private method test, we need to test through public API
        // Arrange
        var text = "test";
        var expectedEmbedding = new float[384];
 
        // Setup providers
        _providerMocks[0].Setup(p => p.CanHandle(_settings)).Returns(false);
        _providerMocks[1].Setup(p => p.CanHandle(_settings)).Returns(true);
        _providerMocks[1].Setup(p => p.GetEmbeddingAsync(text)).ReturnsAsync(new EmbeddingResult(expectedEmbedding));
        _providerMocks[2].Setup(p => p.CanHandle(_settings)).Returns(true); // This should not be selected
 
        // Act
        var result = await _embeddingService.GetEmbeddingAsync(text);
 
        // Assert
        Assert.Equal(expectedEmbedding, result.Vector);
        _providerMocks[0].Verify(p => p.CanHandle(_settings), Times.Once);
        _providerMocks[1].Verify(p => p.CanHandle(_settings), Times.Once);
        _providerMocks[2].Verify(p => p.CanHandle(_settings), Times.Never);
    }
}