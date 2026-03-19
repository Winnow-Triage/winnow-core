using Winnow.Server.Domain.Reports.ValueObjects;
using Winnow.Server.Infrastructure.Configuration;

namespace Winnow.Server.Infrastructure.Analysis;

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
