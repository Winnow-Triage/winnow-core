using Amazon.Comprehend;
using Amazon.Comprehend.Model;
using Microsoft.Extensions.Logging;
using System.Text;
using Winnow.Server.Infrastructure.Configuration;

namespace Winnow.Server.Infrastructure.Analysis;

/// <summary>
/// PII redaction provider using Amazon Comprehend.
/// </summary>
internal class AwsComprehendPiiRedactionProvider(
    IAmazonComprehend comprehendClient,
    ILogger<AwsComprehendPiiRedactionProvider> logger) : IPiiRedactionProvider
{
    private readonly IAmazonComprehend _comprehendClient = comprehendClient;
    private readonly ILogger<AwsComprehendPiiRedactionProvider> _logger = logger;

    public async Task<string> RedactPiiAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        try
        {
            var request = new DetectPiiEntitiesRequest
            {
                Text = text,
                LanguageCode = LanguageCode.En
            };

            var response = await _comprehendClient.DetectPiiEntitiesAsync(request, cancellationToken);

            if (response.Entities.Count == 0)
            {
                return text;
            }

            // Sort by start ascending, then by end descending (greedy: longest first)
            var orderedEntities = response.Entities
                .OrderBy(e => e.BeginOffset)
                .ThenByDescending(e => e.EndOffset)
                .ToList();

            var sb = new StringBuilder();
            int lastIndex = 0;

            foreach (var entity in orderedEntities)
            {
                int begin = entity.BeginOffset ?? 0;
                int end = entity.EndOffset ?? 0;

                // Skip if this entity overlaps with the one we just processed
                if (begin < lastIndex)
                {
                    _logger.LogWarning("AwsComprehendPiiRedactionProvider: Skipping overlapping entity {Type} ({Start}-{End})",
                        entity.Type, begin, end);
                    continue;
                }

                // Append text since last match
                if (begin > lastIndex)
                {
                    sb.Append(text, lastIndex, begin - lastIndex);
                }

                // Append redaction token
                sb.Append('[');
                sb.Append(entity.Type);
                sb.Append(']');

                lastIndex = end;
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
            _logger.LogError(ex, "AWS Comprehend PII redaction failed.");
            return text;
        }
    }

    public bool CanHandle(LlmSettings settings)
    {
        return settings?.PiiRedactionProvider?.Equals("AmazonComprehend", StringComparison.OrdinalIgnoreCase) == true;
    }
}
