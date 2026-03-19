using Winnow.Server.Domain.Reports.ValueObjects;
using Winnow.Server.Infrastructure.Configuration;

namespace Winnow.Server.Infrastructure.Analysis;

/// <summary>
/// No-Op implementation for local toxicity detection.
/// </summary>
internal class LocalToxicityDetectionProvider : IToxicityDetectionProvider
{
    private readonly ILogger<LocalToxicityDetectionProvider> _logger;

    public LocalToxicityDetectionProvider(ILogger<LocalToxicityDetectionProvider> logger)
    {
        _logger = logger;
    }

    public Task<ToxicityScanResult> DetectToxicityAsync(string text, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("LocalToxicityDetectionProvider: Returning no toxicity for provided text.");

        // Return a result with all categories at 0.0 (clean)
        return Task.FromResult(new ToxicityScanResult(0, 0, 0, 0, 0, 0, 0, 0));
    }

    public bool CanHandle(LlmSettings settings)
    {
        return string.IsNullOrEmpty(settings?.ToxicityProvider) ||
               settings.ToxicityProvider.Equals("Local", StringComparison.OrdinalIgnoreCase);
    }
}
