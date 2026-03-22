using Winnow.API.Domain.Ai;
using Winnow.API.Infrastructure.Configuration;

namespace Winnow.API.Services.Ai.Strategies;

/// <summary>
/// A non-AI fallback provider that returns a deterministic dummy vector.
/// Used primarily for testing environments where ONNX models or external APIs are unavailable.
/// </summary>
internal class PlaceholderEmbeddingProvider : IEmbeddingProvider
{
    private readonly ILogger<PlaceholderEmbeddingProvider> _logger;
    private readonly bool _isTestEnv;

    public PlaceholderEmbeddingProvider(ILogger<PlaceholderEmbeddingProvider> logger, IHostEnvironment env)
    {
        _logger = logger;
        _isTestEnv = env.IsDevelopment() || env.EnvironmentName == "Testing" || env.EnvironmentName == "Development";
        _logger.LogInformation("PlaceholderEmbeddingProvider initialized. Environment: {Env}, IsTestEnv: {IsTestEnv}", env.EnvironmentName, _isTestEnv);
    }

    public Task<EmbeddingResult> GetEmbeddingAsync(string text)
    {
        _logger.LogWarning("PlaceholderEmbeddingProvider: Generating dummy embedding for text of length {Length}. This should only happen in Test/Dev environments.", text.Length);

        // Return a deterministic 384-dimensional vector (matches our BERT model size)
        var vector = new float[384];
        for (int i = 0; i < vector.Length; i++)
        {
            vector[i] = 0.1f; // Dummy value
        }

        return Task.FromResult(new EmbeddingResult(vector, new AiUsageInfo(0, 0, "dummy-model", "Placeholder")));
    }

    public bool CanHandle(LlmSettings settings)
    {
        // This provider acts as a global fallback in Test/Dev environments
        // OR if explicitly requested via "Placeholder"
        return _isTestEnv ||
               (settings?.EmbeddingProvider?.Equals("Placeholder", StringComparison.OrdinalIgnoreCase) ?? false);
    }
}
