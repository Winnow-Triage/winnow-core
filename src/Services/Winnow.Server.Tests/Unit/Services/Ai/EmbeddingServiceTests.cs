using Microsoft.Extensions.Logging;
using Moq;
using Winnow.Server.Infrastructure.Configuration;
using Winnow.Server.Services.Ai;
using Winnow.Server.Services.Ai.Strategies;

namespace Winnow.Server.Tests.Unit.Services.Ai;

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
        _providerMocks = new List<Mock<IEmbeddingProvider>>
        {
            new Mock<IEmbeddingProvider>(),
            new Mock<IEmbeddingProvider>(),
            new Mock<IEmbeddingProvider>()
        };

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
    public async Task GetEmbeddingAsync_WithEmptyText_ReturnsMockEmbeddingAndLogs()
    {
        // Arrange
        var emptyText = "";

        // Act
        var result = await _embeddingService.GetEmbeddingAsync(emptyText);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(384, result.Length); // Mock embedding should be 384-dimensional
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("empty text")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)!),
            Times.Once);
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
        _providerMocks[1].Setup(p => p.GetEmbeddingAsync(text)).ReturnsAsync(expectedEmbedding);

        // Third provider can also handle but should not be selected
        _providerMocks[2].Setup(p => p.CanHandle(_settings)).Returns(true);

        // Act
        var result = await _embeddingService.GetEmbeddingAsync(text);

        // Assert
        Assert.Equal(expectedEmbedding, result);
        _providerMocks[0].Verify(p => p.CanHandle(_settings), Times.Once);
        _providerMocks[1].Verify(p => p.CanHandle(_settings), Times.Once);
        _providerMocks[1].Verify(p => p.GetEmbeddingAsync(text), Times.Once);
        _providerMocks[2].Verify(p => p.CanHandle(_settings), Times.Never); // Should not be checked after finding a provider
    }

    [Fact]
    public async Task GetEmbeddingAsync_WhenNoProviderCanHandleSettings_UsesFirstProviderAsFallback()
    {
        // Arrange
        var text = "test text";
        var expectedEmbedding = new float[384];
        var random = new Random();
        for (int i = 0; i < expectedEmbedding.Length; i++) expectedEmbedding[i] = (float)random.NextDouble();

        // No provider can handle settings
        foreach (var mock in _providerMocks)
        {
            mock.Setup(p => p.CanHandle(_settings)).Returns(false);
        }

        // First provider will be used as fallback
        _providerMocks[0].Setup(p => p.GetEmbeddingAsync(text)).ReturnsAsync(expectedEmbedding);

        // Act
        var result = await _embeddingService.GetEmbeddingAsync(text);

        // Assert
        Assert.Equal(expectedEmbedding, result);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No provider can handle settings")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)!),
            Times.Once);
        _providerMocks[0].Verify(p => p.GetEmbeddingAsync(text), Times.Once);
    }

    [Fact]
    public async Task GetEmbeddingAsync_WhenProviderFails_FallsBackToMockEmbedding()
    {
        // Arrange
        var text = "test text";
        var exception = new Exception("Provider failed");

        // First provider can handle but fails
        _providerMocks[0].Setup(p => p.CanHandle(_settings)).Returns(true);
        _providerMocks[0].Setup(p => p.GetEmbeddingAsync(text)).ThrowsAsync(exception);

        // Act
        var result = await _embeddingService.GetEmbeddingAsync(text);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(384, result.Length); // Should return mock embedding
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Provider") && v.ToString()!.Contains("failed")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)!),
            Times.Once);
    }

    [Fact]
    public async Task GetEmbeddingAsync_WhenNoProvidersRegistered_UsesInternalMock()
    {
        // Arrange
        var text = "test text";
        var emptyService = new EmbeddingService(_loggerMock.Object, _settings, Enumerable.Empty<IEmbeddingProvider>());

        // Act
        var result = await emptyService.GetEmbeddingAsync(text);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(384, result.Length); // Should return internal mock embedding
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No embedding providers registered")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)!),
            Times.Once);
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
        _providerMocks[0].Setup(p => p.GetEmbeddingAsync(text)).ReturnsAsync(expectedEmbedding);

        // Act
        var result = await _embeddingService.GetEmbeddingAsync(text);

        // Assert
        Assert.Equal(expectedEmbedding, result);
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
        _providerMocks[1].Setup(p => p.GetEmbeddingAsync(text)).ReturnsAsync(expectedEmbedding);
        _providerMocks[2].Setup(p => p.CanHandle(_settings)).Returns(true); // This should not be selected

        // Act
        var result = await _embeddingService.GetEmbeddingAsync(text);

        // Assert
        Assert.Equal(expectedEmbedding, result);
        _providerMocks[0].Verify(p => p.CanHandle(_settings), Times.Once);
        _providerMocks[1].Verify(p => p.CanHandle(_settings), Times.Once);
        _providerMocks[2].Verify(p => p.CanHandle(_settings), Times.Never);
    }
}