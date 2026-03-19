namespace Winnow.Server.Infrastructure.Analysis;

/// <summary>
/// Interface for PII redaction service.
/// </summary>
public interface IPiiRedactionService
{
    /// <summary>
    /// Redacts PII from the given text.
    /// </summary>
    /// <param name="text">The text to redact.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The redacted text.</returns>
    Task<string> RedactPiiAsync(string text, CancellationToken cancellationToken = default);
}