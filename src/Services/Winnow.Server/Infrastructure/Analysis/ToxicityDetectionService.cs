using Microsoft.Extensions.Options;
using Winnow.Server.Domain.Reports.ValueObjects;
using Winnow.Server.Infrastructure.Configuration;

namespace Winnow.Server.Infrastructure.Analysis;

/// <summary>
/// Orchestrator service for toxicity detection.
/// </summary>
internal class ToxicityDetectionService : IToxicityDetectionService
{
    private readonly IEnumerable<IToxicityDetectionProvider> _providers;
    private readonly LlmSettings _settings;
    private readonly ILogger<ToxicityDetectionService> _logger;

    public ToxicityDetectionService(
        IEnumerable<IToxicityDetectionProvider> providers,
        IOptions<LlmSettings> settings,
        ILogger<ToxicityDetectionService> logger)
    {
        _providers = providers;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<ToxicityScanResult> DetectToxicityAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new ToxicityScanResult(0, 0, 0, 0, 0, 0, 0, 0);
        }

        var providers = _providers.ToList();
        var provider = providers.FirstOrDefault(p => p.CanHandle(_settings)) ?? providers.FirstOrDefault();

        if (provider == null)
        {
            _logger.LogWarning("No toxicity detection providers registered. Returning clean result.");
            return new ToxicityScanResult(0, 0, 0, 0, 0, 0, 0, 0);
        }

        return await provider.DetectToxicityAsync(text, cancellationToken);
    }
}
