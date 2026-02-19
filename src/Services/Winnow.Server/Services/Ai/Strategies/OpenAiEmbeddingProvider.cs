using System.Text.Json;
using Winnow.Server.Infrastructure.Configuration;

namespace Winnow.Server.Services.Ai.Strategies;

/// <summary>
/// OpenAI embedding provider for generating embeddings using OpenAI's API.
/// </summary>
public class OpenAiEmbeddingProvider : IEmbeddingProvider
{
    private readonly ILogger<OpenAiEmbeddingProvider> _logger;
    private readonly HttpClient? _httpClient;
    private readonly string? _apiKey;
    private readonly string? _modelId;
    private readonly Random _rng = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenAiEmbeddingProvider"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="httpClientFactory">The HTTP client factory.</param>
    /// <param name="settings">The LLM settings.</param>
    public OpenAiEmbeddingProvider(
        ILogger<OpenAiEmbeddingProvider> logger,
        IHttpClientFactory httpClientFactory,
        LlmSettings settings)
    {
        _logger = logger;

        if (settings.Provider == "OpenAI" && !string.IsNullOrWhiteSpace(settings.OpenAI?.ApiKey))
        {
            _apiKey = settings.OpenAI.ApiKey;
            _modelId = settings.OpenAI.ModelId;
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
            _logger.LogInformation("OpenAiEmbeddingProvider: Configured for model {ModelId}", _modelId);
        }
        else
        {
            _logger.LogWarning("OpenAiEmbeddingProvider: OpenAI configuration not available or incomplete");
        }
    }

    /// <summary>
    /// Generates an embedding for the given text.
    /// </summary>
    /// <param name="text">The text to generate an embedding for.</param>
    /// <returns>The embedding as a float array.</returns>
    public async Task<float[]> GetEmbeddingAsync(string text)
    {
        if (_httpClient == null || string.IsNullOrEmpty(_apiKey) || string.IsNullOrEmpty(_modelId))
        {
            _logger.LogWarning("OpenAiEmbeddingProvider: Configuration missing, using mock embedding");
            return GenerateMockEmbedding();
        }

        try
        {
            // OpenAI embedding API request
            var requestBody = new
            {
                input = text,
                model = _modelId
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("https://api.openai.com/v1/embeddings", content);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("OpenAiEmbeddingProvider: API request failed with status {StatusCode}", response.StatusCode);
                return GenerateMockEmbedding();
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            var embeddingResponse = JsonSerializer.Deserialize<OpenAiEmbeddingResponse>(responseJson);

            if (embeddingResponse?.Data?[0]?.Embedding == null)
            {
                _logger.LogError("OpenAiEmbeddingProvider: Invalid response format");
                return GenerateMockEmbedding();
            }

            // OpenAI embeddings are typically normalized
            return embeddingResponse.Data[0].Embedding!; // Use null-forgiving operator since we checked above
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenAiEmbeddingProvider: Failed to generate embedding");
            return GenerateMockEmbedding();
        }
    }

    /// <summary>
    /// Checks if this provider can handle the given LLM settings.
    /// </summary>
    /// <param name="settings">The LLM settings.</param>
    /// <returns>True if this provider can handle the settings, false otherwise.</returns>
    public bool CanHandle(LlmSettings settings)
    {
        return settings?.Provider == "OpenAI" &&
               !string.IsNullOrWhiteSpace(settings.OpenAI?.ApiKey) &&
               !string.IsNullOrWhiteSpace(settings.OpenAI.ModelId);
    }

    /// <summary>
    /// Generates a mock embedding for testing purposes.
    /// </summary>
    /// <returns>A mock embedding as a float array.</returns>
    private float[] GenerateMockEmbedding()
    {
        var embedding = new float[384];
        for (int i = 0; i < embedding.Length; i++)
        {
            embedding[i] = (float)(_rng.NextDouble() * 2 - 1);
        }
        return embedding;
    }

    /// <summary>
    /// Represents the response from the OpenAI embeddings API.
    /// </summary>
    private class OpenAiEmbeddingResponse
    {
        public List<OpenAiEmbeddingData>? Data { get; set; }
    }

    /// <summary>
    /// Represents the embedding data from the OpenAI embeddings API.
    /// </summary>
    private class OpenAiEmbeddingData
    {
        public float[]? Embedding { get; set; }
    }
}