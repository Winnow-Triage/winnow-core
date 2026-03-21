using System.Text.Json;
using Winnow.API.Infrastructure.Configuration;

namespace Winnow.API.Services.Ai.Strategies;

/// <summary>
/// Local embedding provider for Ollama.
/// </summary>
internal class LocalEmbeddingProvider : IEmbeddingProvider
{
    private readonly ILogger<LocalEmbeddingProvider> _logger;
    private readonly HttpClient? _httpClient;
    private readonly string? _endpoint;
    private readonly string? _modelId;


    public LocalEmbeddingProvider(
        ILogger<LocalEmbeddingProvider> logger,
        IHttpClientFactory httpClientFactory,
        LlmSettings settings)
    {
        _logger = logger;

        if (settings.EmbeddingProvider == "Ollama" && !string.IsNullOrWhiteSpace(settings.Ollama?.Endpoint))
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
            throw new InvalidOperationException("LocalEmbeddingProvider: Configuration is incomplete (Ollama endpoint or model ID missing).");
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
                throw new HttpRequestException($"LocalEmbeddingProvider: API request failed with status {response.StatusCode}");
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            var embeddingResponse = JsonSerializer.Deserialize<OllamaEmbeddingResponse>(responseJson);

            if (embeddingResponse?.Embedding == null || embeddingResponse.Embedding.Length == 0)
            {
                _logger.LogError("LocalEmbeddingProvider: Invalid response format or empty embedding");
                throw new InvalidDataException("LocalEmbeddingProvider: Invalid response format or empty embedding");
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
            throw;
        }
    }

    public bool CanHandle(LlmSettings settings)
    {
        return settings?.EmbeddingProvider == "Ollama" &&
               !string.IsNullOrWhiteSpace(settings.Ollama?.Endpoint) &&
               !string.IsNullOrWhiteSpace(settings.Ollama.ModelId);
    }



    private sealed class OllamaEmbeddingResponse
    {
        public float[]? Embedding { get; set; }
    }
}