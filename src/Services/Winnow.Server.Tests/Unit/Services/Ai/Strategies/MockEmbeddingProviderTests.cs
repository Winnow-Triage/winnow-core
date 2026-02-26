using Microsoft.Extensions.Logging;
using Moq;
using Winnow.Server.Infrastructure.Configuration;
using Winnow.Server.Services.Ai.Strategies;

namespace Winnow.Server.Tests.Unit.Services.Ai.Strategies;

public class MockEmbeddingProviderTests
{
    private readonly Mock<ILogger<MockEmbeddingProvider>> _loggerMock;
    private readonly MockEmbeddingProvider _provider;

    public MockEmbeddingProviderTests()
    {
        _loggerMock = new Mock<ILogger<MockEmbeddingProvider>>();
        _provider = new MockEmbeddingProvider(_loggerMock.Object);
    }

    [Fact]
    public void Constructor_WhenCalled_InitializesSuccessfully()
    {
        // Arrange & Act done in constructor

        // Assert
        Assert.NotNull(_provider);
    }

    [Fact]
    public async Task GetEmbeddingAsync_AlwaysReturns384DimensionalVector()
    {
        // Arrange
        var text = "This is a test text";

        // Act
        var result = await _provider.GetEmbeddingAsync(text);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(384, result.Length);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Generating mock embedding")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)!),
            Times.Once);
    }

    [Fact]
    public async Task GetEmbeddingAsync_WithDifferentText_ReturnsDifferentEmbeddings()
    {
        // Arrange
        var text1 = "First test text";
        var text2 = "Second test text";

        // Act
        var result1 = await _provider.GetEmbeddingAsync(text1);
        var result2 = await _provider.GetEmbeddingAsync(text2);

        // Assert
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Equal(384, result1.Length);
        Assert.Equal(384, result2.Length);

        // Mock embeddings should be different (random)
        Assert.NotEqual(result1, result2);
    }

    [Fact]
    public async Task GetEmbeddingAsync_EmbeddingValuesAreInExpectedRange()
    {
        // Arrange
        var text = "Test text";

        // Act
        var result = await _provider.GetEmbeddingAsync(text);

        // Assert
        foreach (var value in result)
        {
            Assert.InRange(value, -1.0f, 1.0f);
        }
    }

    [Fact]
    public void CanHandle_WithNullSettings_ReturnsFalse()
    {
        // Arrange
        LlmSettings? settings = null;

        // Act
        var result = _provider.CanHandle(settings!);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CanHandle_WithEmptySettings_ReturnsFalse()
    {
        // Arrange
        var settings = new LlmSettings();

        // Act
        var result = _provider.CanHandle(settings);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CanHandle_WithMockSettings_ReturnsTrue()
    {
        // Arrange
        var settings = new LlmSettings
        {
            EmbeddingProvider = "Mock"
        };

        // Act
        var result = _provider.CanHandle(settings);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CanHandle_WithOtherProvider_ReturnsFalse()
    {
        // Arrange
        var settings = new LlmSettings
        {
            EmbeddingProvider = "Onnx"
        };

        // Act
        var result = _provider.CanHandle(settings);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetEmbeddingAsync_WithVeryLongText_HandlesSuccessfully()
    {
        // Arrange
        var longText = new string('x', 10000);

        // Act
        var result = await _provider.GetEmbeddingAsync(longText);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(384, result.Length);
    }

    [Fact]
    public async Task GetEmbeddingAsync_WithSpecialCharacters_HandlesSuccessfully()
    {
        // Arrange
        var text = "Test with special chars: !@#$%^&*()_+{}|:\"<>?~`-=[]\\;',./";

        // Act
        var result = await _provider.GetEmbeddingAsync(text);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(384, result.Length);
    }

    [Fact]
    public async Task GetEmbeddingAsync_WithEmptyString_HandlesSuccessfully()
    {
        // Arrange
        var text = "";

        // Act
        var result = await _provider.GetEmbeddingAsync(text);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(384, result.Length);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("length 0")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)!),
            Times.Once);
    }
}