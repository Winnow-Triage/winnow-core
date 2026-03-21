using System.Text;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Winnow.Server.Infrastructure.Configuration;

namespace Winnow.Server.Infrastructure.Analysis;

/// <summary>
/// PII redaction provider using Microsoft Presidio Analyzer.
/// </summary>
internal class LocalPiiRedactionProvider(
    HttpClient httpClient,
    IOptions<LlmSettings> settings,
    ILogger<LocalPiiRedactionProvider> logger) : IPiiRedactionProvider
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly LlmSettings _settings = settings.Value;
    private readonly ILogger<LocalPiiRedactionProvider> _logger = logger;

    public async Task<string> RedactPiiAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        try
        {
            var request = new PresidioAnalyzeBody
            {
                Text = text,
                Language = "en",
                ScoreThreshold = 0.4f,
                Entities =
                [
                    "PERSON", "LOCATION", "EMAIL_ADDRESS", "PHONE_NUMBER",
                    "URL", "DATE_TIME", "CREDIT_CARD", "DOMAIN_NAME",
                    "IP_ADDRESS", "US_SSN", "US_BANK_NUMBER"
                ]
            };

            _logger.LogInformation("LocalPiiRedactionProvider: Sending text to Presidio for analysis. (Length: {Length})", text.Length);

            var response = await _httpClient.PostAsJsonAsync(
                $"{_settings.Presidio.AnalyzerEndpoint}/analyze",
                request,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var results = await response.Content.ReadFromJsonAsync<List<PresidioAnalyzeItem>>(cancellationToken: cancellationToken);

            if (results == null || results.Count == 0)
            {
                _logger.LogInformation("LocalPiiRedactionProvider: No PII entities detected by Presidio.");
                return text;
            }

            _logger.LogInformation("LocalPiiRedactionProvider: Presidio detected {Count} PII entities.", results.Count);

            // Sort by start ascending, then by end descending (greedy: longest first)
            var orderedResults = results
                .OrderBy(r => r.Start)
                .ThenByDescending(r => r.End)
                .ToList();

            var sb = new StringBuilder();
            int lastIndex = 0;

            foreach (var result in orderedResults)
            {
                // Skip if this entity overlaps with the one we just processed
                if (result.Start < lastIndex)
                {
                    _logger.LogWarning("LocalPiiRedactionProvider: Skipping overlapping entity {Type} ({Start}-{End})",
                        result.EntityType, result.Start, result.End);
                    continue;
                }

                // Append text since last match
                if (result.Start > lastIndex)
                {
                    sb.Append(text, lastIndex, result.Start - lastIndex);
                }

                // Append redaction token
                sb.Append('[');
                sb.Append(result.EntityType);
                sb.Append(']');

                lastIndex = result.End;
            }

            // Append remaining text
            if (lastIndex < text.Length)
            {
                sb.Append(text, lastIndex, text.Length - lastIndex);
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Local PII redaction failed (Presidio).");
            return text; // Fallback to original text on failure
        }
    }

    public bool CanHandle(LlmSettings settings)
    {
        return settings?.PiiRedactionProvider?.Equals("Local", StringComparison.OrdinalIgnoreCase) == true;
    }

    private class PresidioAnalyzeBody
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

        [JsonPropertyName("language")]
        public string Language { get; set; } = "en";

        [JsonPropertyName("entities")]
        public List<string>? Entities { get; set; }

        [JsonPropertyName("score_threshold")]
        public float? ScoreThreshold { get; set; }
    }

    private class PresidioAnalyzeItem
    {
        [JsonPropertyName("start")]
        public int Start { get; set; }

        [JsonPropertyName("end")]
        public int End { get; set; }

        [JsonPropertyName("entity_type")]
        public string EntityType { get; set; } = string.Empty;

        [JsonPropertyName("score")]
        public float Score { get; set; }
    }
}
