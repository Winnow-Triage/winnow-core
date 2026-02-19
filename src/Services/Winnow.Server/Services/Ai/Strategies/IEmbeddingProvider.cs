using Winnow.Server.Infrastructure.Configuration;

namespace Winnow.Server.Services.Ai.Strategies;

/// <summary>
/// Interface for embedding providers.
/// </summary>
public interface IEmbeddingProvider
{
    /// <summary>
    /// Generates an embedding vector for the given text.
    /// </summary>
    /// <param name="text">The text to embed.</param>
    /// <returns>A 384-dimensional float array representing the embedding.</returns>
    Task<float[]> GetEmbeddingAsync(string text);

    /// <summary>
    /// Determines if this provider can handle the given LLM settings.
    /// </summary>
    /// <param name="settings">The LLM settings to evaluate.</param>
    /// <returns>True if this provider can handle the settings, false otherwise.</returns>
    bool CanHandle(LlmSettings settings);
}