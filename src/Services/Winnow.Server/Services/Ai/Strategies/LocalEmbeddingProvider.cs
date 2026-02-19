using System.Text.Json;
using Winnow.Server.Infrastructure.Configuration;

namespace Winnow.Server.Services.Ai.Strategies;

/// <summary>
/// Local embedding provider for Ollama.
/// </summary>
public class LocalEmbeddingProvider : IEmbeddingProvider
{
    private readonly ILogger<LocalEmbeddingProvider> _logger;
    private readonly HttpClient? _httpClient;
    private readonly string? _endpoint;
    private readonly string? _modelId;
    private readonly Random _rng = new();

    public LocalEmbeddingProvider(
        ILogger<LocalEmbeddingProvider> logger,
        IHttpClientFactory httpClientFactory,
        LlmSettings settings)
    {
        _logger = logger;

        if (settings.Provider == "Ollama" && !string.IsNullOrWhiteSpace(settings.Ollama?.Endpoint))
        {
            _endpoint = settings.Ollama.Endpoint;
            _modelId = settings.Ollama.ModelId;
            _httpClient = httpClientFactory.CreateClient();
            _logger.LogInformation("LocalEmbeddingProvider: Configured for model {ModelId} at {Endpoint}", _modelId, _endpoint);
        }
        else
        {
            _logger.LogWarning("LocalEmbeddingProvider: Ollama configuration not available or incomplete");
        }
    }

    public async Task<float[]> GetEmbeddingAsync(string text)
    {
        if (_httpClient == null || string.IsNullOrEmpty(_endpoint) || string.IsNullOrEmpty(_modelId))
        {
            _logger.LogWarning("LocalEmbeddingProvider: Configuration missing, using mock embedding");
            return GenerateMockEmbedding();
        }

        try
        {
            // Ollama embedding API request
            var requestBody = new
            {
                model = _modelId,
                prompt = text
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_endpoint}/api/embeddings", content);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("LocalEmbeddingProvider: API request failed with status {StatusCode}", response.StatusCode);
                return GenerateMockEmbedding();
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            var embeddingResponse = JsonSerializer.Deserialize<OllamaEmbeddingResponse>(responseJson);

            if (embeddingResponse?.Embedding == null || embeddingResponse.Embedding.Length == 0)
            {
                _logger.LogError("LocalEmbeddingProvider: Invalid response format or empty embedding");
                return GenerateMockEmbedding();
            }

            // Ollama embeddings might need normalization
            var embedding = embeddingResponse.Embedding;
            
            // Normalize the embedding
            float norm = 0;
            for (int i = 0; i < embedding.Length; i++) norm += embedding[i] * embedding[i];
            norm = (float)Math.Sqrt(norm);

            if (norm > 1e-6)
            {
                for (int i = 0; i < embedding.Length; i++) embedding[i] /= norm;
            }

            return embedding;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LocalEmbeddingProvider: Failed to generate embedding");
            return GenerateMockEmbedding();
        }
    }

    public bool CanHandle(LlmSettings settings)
    {
        return settings?.Provider == "Ollama" && 
               !string.IsNullOrWhiteSpace(settings.Ollama?.Endpoint) &&
               !string.IsNullOrWhiteSpace(settings.Ollama.ModelId);
    }

    private float[] GenerateMockEmbedding()
    {
        var embedding = new float[384];
        for (int i = 0; i < embedding.Length; i++)
        {
            embedding[i] = (float)(_rng.NextDouble() * 2 - 1);
        }
        return embedding;
    }

    private class OllamaEmbeddingResponse
    {
        public float[]? Embedding { get; set; }
    }
}