using System.Text.Json;
using Winnow.API.Infrastructure.Configuration;

namespace Winnow.API.Services.Ai.Strategies;

/// <summary>
/// OpenAI embedding provider for generating embeddings using OpenAI's API.
/// </summary>
internal class OpenAiEmbeddingProvider : IEmbeddingProvider
{
    private readonly ILogger<OpenAiEmbeddingProvider> _logger;
    private readonly HttpClient? _httpClient;
    private readonly string? _apiKey;
    private readonly string? _modelId;

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

        if (settings.EmbeddingProvider == "OpenAI" && !string.IsNullOrWhiteSpace(settings.OpenAI?.ApiKey))
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

    public async Task<float[]> GetEmbeddingAsync(string text)
    {
        if (_httpClient == null || string.IsNullOrEmpty(_apiKey) || string.IsNullOrEmpty(_modelId))
        {
            throw new InvalidOperationException("OpenAiEmbeddingProvider: Configuration is incomplete (API key or model ID missing).");
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
                throw new HttpRequestException($"OpenAiEmbeddingProvider: API request failed with status {response.StatusCode}");
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            var embeddingResponse = JsonSerializer.Deserialize<OpenAiEmbeddingResponse>(responseJson);

            if (embeddingResponse?.Data?[0]?.Embedding == null)
            {
                throw new InvalidOperationException("OpenAiEmbeddingProvider: Received an invalid or empty response from OpenAI API.");
            }

            // OpenAI embeddings are typically normalized
            return embeddingResponse.Data[0].Embedding!; // Use null-forgiving operator since we checked above
        }
        catch (Exception ex) when (ex is not InvalidOperationException && ex is not HttpRequestException)
        {
            _logger.LogError(ex, "OpenAiEmbeddingProvider: Failed to generate embedding");
            throw;
        }
    }

    /// <summary>
    /// Checks if this provider can handle the given LLM settings.
    /// </summary>
    /// <param name="settings">The LLM settings.</param>
    /// <returns>True if this provider can handle the settings, false otherwise.</returns>
    public bool CanHandle(LlmSettings settings)
    {
        return settings?.EmbeddingProvider == "OpenAI" &&
               !string.IsNullOrWhiteSpace(settings.OpenAI?.ApiKey) &&
               !string.IsNullOrWhiteSpace(settings.OpenAI.ModelId);
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