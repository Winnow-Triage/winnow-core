using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Text.Json;
using Winnow.Server.Infrastructure.Configuration;
using Winnow.Server.Services.Ai.Strategies;

namespace Winnow.Server.Tests.Unit.Services.Ai;

public class OnnxEmbeddingProviderTests
{
    private readonly Mock<ILogger<OnnxEmbeddingProvider>> _loggerMock;
    private readonly Mock<IHostEnvironment> _hostEnvMock;
    private readonly string _testContentRootPath;

    public OnnxEmbeddingProviderTests()
    {
        _loggerMock = new Mock<ILogger<OnnxEmbeddingProvider>>();
        _hostEnvMock = new Mock<IHostEnvironment>();
        _testContentRootPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testContentRootPath);
        _hostEnvMock.Setup(x => x.ContentRootPath).Returns(_testContentRootPath);
    }

    [Fact]
    public void Constructor_WhenNoModelFilesExist_InitializesWithoutSession()
    {
        // Arrange
        var aiModelDir = Path.Combine(_testContentRootPath, "AiModel");
        Directory.CreateDirectory(aiModelDir);
        // No model.onnx or vocab.txt files exist

        // Act
        var provider = new OnnxEmbeddingProvider(_loggerMock.Object, _hostEnvMock.Object);

        // Assert
        // Should not throw and should handle missing files gracefully
        Assert.NotNull(provider);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("ONNX model not found")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)!),
            Times.Once);
    }

    [Fact(Skip = "ONNX runtime may not be available in test environment")]
    public void Constructor_WhenModelFilesExist_InitializesWithSession()
    {
        // Arrange
        var aiModelDir = Path.Combine(_testContentRootPath, "AiModel");
        Directory.CreateDirectory(aiModelDir);
        var modelPath = Path.Combine(aiModelDir, "model.onnx");
        var vocabPath = Path.Combine(aiModelDir, "vocab.txt");
        
        // Create dummy files
        File.WriteAllText(modelPath, "dummy onnx content");
        File.WriteAllText(vocabPath, "dummy\nvocab\ncontent");

        try
        {
            // Act
            var provider = new OnnxEmbeddingProvider(_loggerMock.Object, _hostEnvMock.Object);

            // Assert
            Assert.NotNull(provider);
            // This test is skipped because ONNX runtime may not be available
            // In a real environment, we would verify successful loading
        }
        finally
        {
            // Cleanup
            if (File.Exists(modelPath)) File.Delete(modelPath);
            if (File.Exists(vocabPath)) File.Delete(vocabPath);
        }
    }

    [Fact]
    public async Task GetEmbeddingAsync_WhenNoSession_ReturnsMockEmbedding()
    {
        // Arrange
        var provider = new OnnxEmbeddingProvider(_loggerMock.Object, _hostEnvMock.Object);
        var text = "Test text for embedding";

        // Act
        var result = await provider.GetEmbeddingAsync(text);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(384, result.Length); // Mock embedding should be 384-dimensional
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Using MOCK embedding")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)!),
            Times.Once);
    }

    [Fact]
    public void CanHandle_WithEmptyOrNullProvider_ReturnsTrue()
    {
        // Arrange
        var provider = new OnnxEmbeddingProvider(_loggerMock.Object, _hostEnvMock.Object);
        
        var nullSettings = new LlmSettings { Provider = null! };
        var emptySettings = new LlmSettings { Provider = "" };
        var placeholderSettings = new LlmSettings { Provider = "Placeholder" };

        // Act & Assert
        Assert.True(provider.CanHandle(nullSettings));
        Assert.True(provider.CanHandle(emptySettings));
        Assert.True(provider.CanHandle(placeholderSettings));
    }

    [Fact]
    public void CanHandle_WithOtherProviders_ReturnsFalse()
    {
        // Arrange
        var provider = new OnnxEmbeddingProvider(_loggerMock.Object, _hostEnvMock.Object);
        
        var ollamaSettings = new LlmSettings { Provider = "Ollama" };
        var openAiSettings = new LlmSettings { Provider = "OpenAI" };

        // Act & Assert
        Assert.False(provider.CanHandle(ollamaSettings));
        Assert.False(provider.CanHandle(openAiSettings));
    }

    [Fact]
    public void CanHandle_WithPlaceholderProvider_ReturnsTrue()
    {
        // Arrange
        var provider = new OnnxEmbeddingProvider(_loggerMock.Object, _hostEnvMock.Object);
        var settings = new LlmSettings { Provider = "Placeholder" };

        // Act
        var result = provider.CanHandle(settings);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Dispose_WhenCalled_DoesNotThrow()
    {
        // Arrange
        var provider = new OnnxEmbeddingProvider(_loggerMock.Object, _hostEnvMock.Object);

        // Act & Assert
        var exception = Record.Exception(() => provider.Dispose());
        Assert.Null(exception);
    }
}

public class LocalEmbeddingProviderTests
{
    private readonly Mock<ILogger<LocalEmbeddingProvider>> _loggerMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;

    public LocalEmbeddingProviderTests()
    {
        _loggerMock = new Mock<ILogger<LocalEmbeddingProvider>>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        // Provider calls CreateClient() extension method (parameterless), which internally calls CreateClient(string.Empty)
        _httpClientFactoryMock.Setup(x => x.CreateClient(string.Empty)).Returns(_httpClient);
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(_httpClient);
    }

    [Fact]
    public void Constructor_WithValidOllamaConfig_InitializesSuccessfully()
    {
        // Arrange
        var settings = new LlmSettings
        {
            Provider = "Ollama",
            Ollama = new OllamaSettings
            {
                Endpoint = "http://localhost:11434",
                ModelId = "llama3"
            }
        };

        // Act
        var provider = new LocalEmbeddingProvider(_loggerMock.Object, _httpClientFactoryMock.Object, settings);

        // Assert
        Assert.NotNull(provider);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Configured for model llama3 at http://localhost:11434")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)!),
            Times.Once);
    }

    [Fact]
    public void Constructor_WithIncompleteOllamaConfig_LogsWarning()
    {
        // Arrange
        var settings = new LlmSettings
        {
            Provider = "Ollama",
            Ollama = new OllamaSettings { Endpoint = "" } // Missing endpoint
        };

        // Act
        var provider = new LocalEmbeddingProvider(_loggerMock.Object, _httpClientFactoryMock.Object, settings);

        // Assert
        Assert.NotNull(provider);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Ollama configuration not available or incomplete")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)!),
            Times.Once);
    }

    [Fact]
    public void Constructor_WithNonOllamaProvider_LogsWarning()
    {
        // Arrange
        var settings = new LlmSettings
        {
            Provider = "OpenAI" // Not Ollama
        };

        // Act
        var provider = new LocalEmbeddingProvider(_loggerMock.Object, _httpClientFactoryMock.Object, settings);

        // Assert
        Assert.NotNull(provider);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Ollama configuration not available or incomplete")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)!),
            Times.Once);
    }

    [Fact]
    public async Task GetEmbeddingAsync_WithMissingConfig_ReturnsMockEmbedding()
    {
        // Arrange
        var settings = new LlmSettings
        {
            Provider = "Ollama",
            Ollama = new OllamaSettings { Endpoint = "" } // Missing endpoint, will log warning
        };
        var provider = new LocalEmbeddingProvider(_loggerMock.Object, _httpClientFactoryMock.Object, settings);
        var text = "Test text";

        // Act
        var result = await provider.GetEmbeddingAsync(text);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(384, result.Length);
        // Should use mock embedding since HTTP client is null
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Ollama configuration not available or incomplete")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)!),
            Times.Once);
    }

    [Fact(Skip = "Optional test - HTTP mocking requires additional setup")]
    public async Task GetEmbeddingAsync_WithSuccessfulApiResponse_ReturnsNormalizedEmbedding()
    {
        // Arrange
        var settings = new LlmSettings
        {
            Provider = "Ollama",
            Ollama = new OllamaSettings
            {
                Endpoint = "http://localhost:11434",
                ModelId = "llama3"
            }
        };
        
        var provider = new LocalEmbeddingProvider(_loggerMock.Object, _httpClientFactoryMock.Object, settings);
        var text = "Test text";
        
        // Setup mock HTTP response with 384-dimensional embedding to match mock fallback size
        var mockEmbedding = new float[384];
        for (int i = 0; i < mockEmbedding.Length; i++)
        {
            mockEmbedding[i] = (float)((i % 10) * 0.1f); // Some pattern for testing
        }
        var responseJson = JsonSerializer.Serialize(new { embedding = mockEmbedding });
        var responseMessage = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson)
        };
        
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(responseMessage);

        // Act
        var result = await provider.GetEmbeddingAsync(text);

        // Assert
        Assert.NotNull(result);
        // The provider normalizes the embedding before returning it
        // So magnitude should be ~1 (normalized)
        float magnitudeSquared = result.Sum(x => x * x);
        Assert.InRange(magnitudeSquared, 0.99f, 1.01f);
    }

    [Fact]
    public async Task GetEmbeddingAsync_WithApiError_ReturnsMockEmbedding()
    {
        // Arrange
        var settings = new LlmSettings
        {
            Provider = "Ollama",
            Ollama = new OllamaSettings
            {
                Endpoint = "http://localhost:11434",
                ModelId = "llama3"
            }
        };
        
        var provider = new LocalEmbeddingProvider(_loggerMock.Object, _httpClientFactoryMock.Object, settings);
        var text = "Test text";
        
        // Setup mock HTTP error response
        var responseMessage = new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError);
        
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(responseMessage);

        // Act
        var result = await provider.GetEmbeddingAsync(text);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(384, result.Length); // Mock embedding size
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("API request failed with status")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)!),
            Times.Once);
    }

    [Fact]
    public void CanHandle_WithValidOllamaConfig_ReturnsTrue()
    {
        // Arrange
        var provider = new LocalEmbeddingProvider(_loggerMock.Object, _httpClientFactoryMock.Object, new LlmSettings());
        var settings = new LlmSettings
        {
            Provider = "Ollama",
            Ollama = new OllamaSettings
            {
                Endpoint = "http://localhost:11434",
                ModelId = "llama3"
            }
        };

        // Act
        var result = provider.CanHandle(settings);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CanHandle_WithMissingEndpoint_ReturnsFalse()
    {
        // Arrange
        var provider = new LocalEmbeddingProvider(_loggerMock.Object, _httpClientFactoryMock.Object, new LlmSettings());
        var settings = new LlmSettings
        {
            Provider = "Ollama",
            Ollama = new OllamaSettings
            {
                Endpoint = "", // Missing endpoint
                ModelId = "llama3"
            }
        };

        // Act
        var result = provider.CanHandle(settings);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CanHandle_WithMissingModelId_ReturnsFalse()
    {
        // Arrange
        var provider = new LocalEmbeddingProvider(_loggerMock.Object, _httpClientFactoryMock.Object, new LlmSettings());
        var settings = new LlmSettings
        {
            Provider = "Ollama",
            Ollama = new OllamaSettings
            {
                Endpoint = "http://localhost:11434",
                ModelId = "" // Missing model ID
            }
        };

        // Act
        var result = provider.CanHandle(settings);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CanHandle_WithNonOllamaProvider_ReturnsFalse()
    {
        // Arrange
        var provider = new LocalEmbeddingProvider(_loggerMock.Object, _httpClientFactoryMock.Object, new LlmSettings());
        var settings = new LlmSettings
        {
            Provider = "OpenAI" // Not Ollama
        };

        // Act
        var result = provider.CanHandle(settings);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CanHandle_WithNullSettings_ReturnsFalse()
    {
        // Arrange
        var provider = new LocalEmbeddingProvider(_loggerMock.Object, _httpClientFactoryMock.Object, new LlmSettings());
        LlmSettings? settings = null;

        // Act
        var result = provider.CanHandle(settings!);

        // Assert
        Assert.False(result);
    }
}

public class OpenAiEmbeddingProviderTests
{
    private readonly Mock<ILogger<OpenAiEmbeddingProvider>> _loggerMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;

    public OpenAiEmbeddingProviderTests()
    {
        _loggerMock = new Mock<ILogger<OpenAiEmbeddingProvider>>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        // Provider calls CreateClient() extension method (parameterless), which internally calls CreateClient(string.Empty)
        _httpClientFactoryMock.Setup(x => x.CreateClient(string.Empty)).Returns(_httpClient);
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(_httpClient);
    }

    [Fact]
    public void Constructor_WithValidOpenAiConfig_InitializesSuccessfully()
    {
        // Arrange
        var settings = new LlmSettings
        {
            Provider = "OpenAI",
            OpenAI = new OpenAiSettings
            {
                ApiKey = "test-api-key-123",
                ModelId = "gpt-4o"
            }
        };

        // Act
        var provider = new OpenAiEmbeddingProvider(_loggerMock.Object, _httpClientFactoryMock.Object, settings);

        // Assert
        Assert.NotNull(provider);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Configured for model gpt-4o")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)!),
            Times.Once);
    }

    [Fact]
    public void Constructor_WithMissingApiKey_LogsWarning()
    {
        // Arrange
        var settings = new LlmSettings
        {
            Provider = "OpenAI",
            OpenAI = new OpenAiSettings { ApiKey = "" } // Missing API key
        };

        // Act
        var provider = new OpenAiEmbeddingProvider(_loggerMock.Object, _httpClientFactoryMock.Object, settings);

        // Assert
        Assert.NotNull(provider);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("OpenAI configuration not available or incomplete")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)!),
            Times.Once);
    }

    [Fact]
    public void Constructor_WithNonOpenAiProvider_LogsWarning()
    {
        // Arrange
        var settings = new LlmSettings
        {
            Provider = "Ollama" // Not OpenAI
        };

        // Act
        var provider = new OpenAiEmbeddingProvider(_loggerMock.Object, _httpClientFactoryMock.Object, settings);

        // Assert
        Assert.NotNull(provider);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("OpenAI configuration not available or incomplete")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)!),
            Times.Once);
    }

    [Fact]
    public async Task GetEmbeddingAsync_WithMissingConfig_ReturnsMockEmbedding()
    {
        // Arrange
        var settings = new LlmSettings { Provider = "OpenAI" }; // Missing OpenAI config
        var provider = new OpenAiEmbeddingProvider(_loggerMock.Object, _httpClientFactoryMock.Object, settings);
        var text = "Test text";

        // Act
        var result = await provider.GetEmbeddingAsync(text);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(384, result.Length);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Configuration missing, using mock embedding")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)!),
            Times.Once);
    }

    [Fact(Skip = "Optional test - HTTP mocking requires additional setup")]
    public async Task GetEmbeddingAsync_WithSuccessfulApiResponse_ReturnsEmbedding()
    {
        // Arrange
        var settings = new LlmSettings
        {
            Provider = "OpenAI",
            OpenAI = new OpenAiSettings
            {
                ApiKey = "test-api-key-123",
                ModelId = "gpt-4o"
            }
        };
        
        var provider = new OpenAiEmbeddingProvider(_loggerMock.Object, _httpClientFactoryMock.Object, settings);
        var text = "Test text";
        
        // Setup mock HTTP response with 384-dimensional embedding
        var mockEmbedding = new float[384];
        for (int i = 0; i < mockEmbedding.Length; i++)
        {
            mockEmbedding[i] = (float)((i % 10) * 0.1f); // Some pattern for testing
        }
        var responseJson = JsonSerializer.Serialize(new
        {
            data = new[]
            {
                new { embedding = mockEmbedding }
            }
        });
        var responseMessage = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson)
        };
        
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(responseMessage);

        // Act
        var result = await provider.GetEmbeddingAsync(text);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(mockEmbedding, result);
    }

    [Fact]
    public async Task GetEmbeddingAsync_WithApiError_ReturnsMockEmbedding()
    {
        // Arrange
        var settings = new LlmSettings
        {
            Provider = "OpenAI",
            OpenAI = new OpenAiSettings
            {
                ApiKey = "test-api-key-123",
                ModelId = "gpt-4o"
            }
        };
        
        var provider = new OpenAiEmbeddingProvider(_loggerMock.Object, _httpClientFactoryMock.Object, settings);
        var text = "Test text";
        
        // Setup mock HTTP error response
        var responseMessage = new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError);
        
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(responseMessage);

        // Act
        var result = await provider.GetEmbeddingAsync(text);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(384, result.Length); // Mock embedding size
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("API request failed with status")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)!),
            Times.Once);
    }

    [Fact]
    public void CanHandle_WithValidOpenAiConfig_ReturnsTrue()
    {
        // Arrange
        var provider = new OpenAiEmbeddingProvider(_loggerMock.Object, _httpClientFactoryMock.Object, new LlmSettings());
        var settings = new LlmSettings
        {
            Provider = "OpenAI",
            OpenAI = new OpenAiSettings
            {
                ApiKey = "test-api-key-123",
                ModelId = "gpt-4o"
            }
        };

        // Act
        var result = provider.CanHandle(settings);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CanHandle_WithMissingApiKey_ReturnsFalse()
    {
        // Arrange
        var provider = new OpenAiEmbeddingProvider(_loggerMock.Object, _httpClientFactoryMock.Object, new LlmSettings());
        var settings = new LlmSettings
        {
            Provider = "OpenAI",
            OpenAI = new OpenAiSettings
            {
                ApiKey = "", // Missing API key
                ModelId = "gpt-4o"
            }
        };

        // Act
        var result = provider.CanHandle(settings);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CanHandle_WithMissingModelId_ReturnsFalse()
    {
        // Arrange
        var provider = new OpenAiEmbeddingProvider(_loggerMock.Object, _httpClientFactoryMock.Object, new LlmSettings());
        var settings = new LlmSettings
        {
            Provider = "OpenAI",
            OpenAI = new OpenAiSettings
            {
                ApiKey = "test-api-key-123",
                ModelId = "" // Missing model ID
            }
        };

        // Act
        var result = provider.CanHandle(settings);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CanHandle_WithNonOpenAiProvider_ReturnsFalse()
    {
        // Arrange
        var provider = new OpenAiEmbeddingProvider(_loggerMock.Object, _httpClientFactoryMock.Object, new LlmSettings());
        var settings = new LlmSettings
        {
            Provider = "Ollama" // Not OpenAI
        };

        // Act
        var result = provider.CanHandle(settings);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CanHandle_WithNullSettings_ReturnsFalse()
    {
        // Arrange
        var provider = new OpenAiEmbeddingProvider(_loggerMock.Object, _httpClientFactoryMock.Object, new LlmSettings());
        LlmSettings? settings = null;

        // Act
        var result = provider.CanHandle(settings!);

        // Assert
        Assert.False(result);
    }
}