using Winnow.Server.Domain.Reports.ValueObjects;

namespace Winnow.Server.Infrastructure.Analysis;

/// <summary>
/// Interface for PII redaction service.
/// </summary>
public interface IToxicityDetectionService
{
    /// <summary>
    /// Detects toxicity in the given text.
    /// </summary>
    /// <param name="text">The text to analyze.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The toxicity analysis result.</returns>
    Task<ToxicityScanResult> DetectToxicityAsync(string text, CancellationToken cancellationToken = default);
}