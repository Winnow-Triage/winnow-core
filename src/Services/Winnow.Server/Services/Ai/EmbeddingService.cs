using Winnow.Server.Infrastructure.Configuration;
using Winnow.Server.Services.Ai.Strategies;

namespace Winnow.Server.Services.Ai;

public interface IEmbeddingService
{
    Task<float[]> GetEmbeddingAsync(string text);
}

public class EmbeddingService : IEmbeddingService
{
    private readonly ILogger<EmbeddingService> _logger;
    private readonly LlmSettings _settings;
    private readonly IEnumerable<IEmbeddingProvider> _providers;

    public EmbeddingService(
        ILogger<EmbeddingService> logger,
        LlmSettings settings,
        IEnumerable<IEmbeddingProvider> providers)
    {
        _logger = logger;
        _settings = settings;
        _providers = providers;
        _logger.LogInformation("EmbeddingService: Initialized with {ProviderCount} providers", providers.Count());
    }

    public async Task<float[]> GetEmbeddingAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            _logger.LogWarning("EmbeddingService: Received empty text, returning mock embedding");
            return GenerateMockEmbedding();
        }

        var provider = SelectProvider();
        _logger.LogDebug("EmbeddingService: Selected provider {ProviderType} for embedding generation",
            provider.GetType().Name);

        try
        {
            return await provider.GetEmbeddingAsync(text);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "EmbeddingService: Provider {ProviderType} failed, falling back to mock",
                provider.GetType().Name);
            return GenerateMockEmbedding();
        }
    }

    private IEmbeddingProvider SelectProvider()
    {
        // Find the first provider that can handle the current settings
        foreach (var provider in _providers)
        {
            if (provider.CanHandle(_settings))
            {
                _logger.LogDebug("EmbeddingService: Provider {ProviderType} can handle settings",
                    provider.GetType().Name);
                return provider;
            }
        }

        // If no provider can handle the settings, use the first available one
        var fallback = _providers.FirstOrDefault();
        if (fallback != null)
        {
            _logger.LogWarning("EmbeddingService: No provider can handle settings, using fallback {ProviderType}",
                fallback.GetType().Name);
            return fallback;
        }

        // If no providers are registered, create a mock provider
        _logger.LogError("EmbeddingService: No embedding providers registered, using internal mock");
        return new MockEmbeddingProvider();
    }

    private static float[] GenerateMockEmbedding()
    {
        var rng = new Random();
        var embedding = new float[384];
        for (int i = 0; i < embedding.Length; i++)
        {
            embedding[i] = (float)(rng.NextDouble() * 2 - 1);
        }
        return embedding;
    }

    private class MockEmbeddingProvider : IEmbeddingProvider
    {
        private readonly Random _rng = new();

        public Task<float[]> GetEmbeddingAsync(string text)
        {
            var embedding = new float[384];
            for (int i = 0; i < embedding.Length; i++)
            {
                embedding[i] = (float)(_rng.NextDouble() * 2 - 1);
            }
            return Task.FromResult(embedding);
        }

        public bool CanHandle(LlmSettings settings) => true;
    }
}