using Microsoft.Extensions.Options;
using Winnow.API.Infrastructure.Configuration;

namespace Winnow.API.Infrastructure.Analysis;

/// <summary>
/// Orchestrator service for PII redaction.
/// </summary>
internal class PiiRedactionService : IPiiRedactionService
{
    private readonly IEnumerable<IPiiRedactionProvider> _providers;
    private readonly LlmSettings _settings;
    private readonly ILogger<PiiRedactionService> _logger;

    public PiiRedactionService(
        IEnumerable<IPiiRedactionProvider> providers,
        IOptions<LlmSettings> settings,
        ILogger<PiiRedactionService> logger)
    {
        _providers = providers;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<string> RedactPiiAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        var providers = _providers.ToList();
        var provider = providers.FirstOrDefault(p => p.CanHandle(_settings)) ?? providers.FirstOrDefault();

        if (provider == null)
        {
            _logger.LogWarning("No PII redaction providers registered. Returning original text.");
            return text;
        }

        return await provider.RedactPiiAsync(text, cancellationToken);
    }
}
