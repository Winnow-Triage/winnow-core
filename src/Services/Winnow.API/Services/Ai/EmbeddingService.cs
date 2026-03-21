using Winnow.API.Infrastructure.Configuration;
using Winnow.API.Services.Ai.Strategies;

namespace Winnow.API.Services.Ai;

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
            throw new ArgumentException("Text cannot be null or empty for embedding generation.", nameof(text));
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
            _logger.LogError(ex, "EmbeddingService: Provider {ProviderType} failed.",
                provider.GetType().Name);
            throw;
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

        // If no provider can handle the settings, throw. We no longer support silent mock fallbacks.
        _logger.LogError("EmbeddingService: No embedding providers registered or configured to handle the current settings (Provider: {Provider}, EmbeddingProvider: {EmbeddingProvider})",
            _settings.Provider, _settings.EmbeddingProvider);

        throw new InvalidOperationException("No suitable embedding provider found. Please check your LlmSettings configuration.");
    }
}