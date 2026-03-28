using Winnow.API.Domain.Ai;
using Winnow.API.Infrastructure.Configuration;

namespace Winnow.API.Services.Ai.Strategies;

public record EmbeddingResult(float[] Vector, AiUsageInfo? Usage = null);

/// <summary>
/// Interface for embedding providers.
/// </summary>
public interface IEmbeddingProvider
{
    /// <summary>
    /// Generates an embedding vector for the given text.
    /// </summary>
    /// <param name="text">The text to embed.</param>
    /// <returns>An EmbeddingResult containing the vector and optional usage info.</returns>
    Task<EmbeddingResult> GetEmbeddingAsync(string text);

    /// <summary>
    /// Determines if this provider can handle the given LLM settings.
    /// </summary>
    /// <param name="settings">The LLM settings to evaluate.</param>
    /// <returns>True if this provider can handle the settings, false otherwise.</returns>
    bool CanHandle(LlmSettings settings);
}