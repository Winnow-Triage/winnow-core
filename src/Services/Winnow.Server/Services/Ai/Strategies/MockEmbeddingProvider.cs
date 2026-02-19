using Winnow.Server.Infrastructure.Configuration;

namespace Winnow.Server.Services.Ai.Strategies;

/// <summary>
/// Mock embedding provider for testing and fallback scenarios.
/// </summary>
internal class MockEmbeddingProvider(ILogger<MockEmbeddingProvider> _logger) : IEmbeddingProvider
{
    private readonly Random _rng = new();

    public Task<float[]> GetEmbeddingAsync(string text)
    {
        _logger.LogDebug("MockEmbeddingProvider: Generating mock embedding for text of length {Length}", text.Length);
        return Task.FromResult(GenerateMockEmbedding());
    }

    public bool CanHandle(LlmSettings settings)
    {
        // Mock provider can always handle any settings as a fallback
        return true;
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
}