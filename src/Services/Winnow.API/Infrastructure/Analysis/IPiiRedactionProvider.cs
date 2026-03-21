using Winnow.API.Infrastructure.Configuration;

namespace Winnow.API.Infrastructure.Analysis;

/// <summary>
/// Internal interface for PII redaction providers.
/// </summary>
internal interface IPiiRedactionProvider
{
    /// <summary>
    /// Redacts PII from the given text.
    /// </summary>
    Task<string> RedactPiiAsync(string text, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if this provider can handle the current settings.
    /// </summary>
    bool CanHandle(LlmSettings settings);
}
