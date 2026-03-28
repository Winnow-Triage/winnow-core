using Winnow.API.Domain.Reports.ValueObjects;
using Winnow.API.Infrastructure.Configuration;

namespace Winnow.API.Infrastructure.Analysis;

/// <summary>
/// Internal interface for toxicity detection providers.
/// </summary>
internal interface IToxicityDetectionProvider
{
    /// <summary>
    /// Detects toxicity in the given text.
    /// </summary>
    Task<ToxicityScanResult> DetectToxicityAsync(string text, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if this provider can handle the current settings.
    /// </summary>
    bool CanHandle(LlmSettings settings);
}
