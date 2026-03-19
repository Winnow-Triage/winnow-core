using Amazon.Comprehend;
using Amazon.Comprehend.Model;
using Microsoft.Extensions.Logging;
using Winnow.Server.Domain.Reports.ValueObjects;
using Winnow.Server.Infrastructure.Configuration;

namespace Winnow.Server.Infrastructure.Analysis;

/// <summary>
/// Toxicity detection provider using Amazon Comprehend.
/// </summary>
internal class AwsComprehendToxicityDetectionProvider : IToxicityDetectionProvider
{
    private readonly IAmazonComprehend _comprehendClient;
    private readonly ILogger<AwsComprehendToxicityDetectionProvider> _logger;

    public AwsComprehendToxicityDetectionProvider(
        IAmazonComprehend comprehendClient,
        ILogger<AwsComprehendToxicityDetectionProvider> logger)
    {
        _comprehendClient = comprehendClient;
        _logger = logger;
    }

    public async Task<ToxicityScanResult> DetectToxicityAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new ToxicityScanResult(0, 0, 0, 0, 0, 0, 0, 0);
        }

        try
        {
            var request = new DetectToxicContentRequest
            {
                TextSegments = new List<TextSegment> { new TextSegment { Text = text } },
                LanguageCode = LanguageCode.En
            };

            var response = await _comprehendClient.DetectToxicContentAsync(request, cancellationToken);
            var result = response.ResultList.FirstOrDefault();

            if (result == null)
            {
                return new ToxicityScanResult(0, 0, 0, 0, 0, 0, 0, 0);
            }

            float overall = 0;
            float graphic = 0;
            float hateSpeech = 0;
            float harassment = 0;
            float insult = 0;
            float profanity = 0;
            float sexual = 0;
            float violence = 0;

            foreach (var label in result.Labels)
            {
                float score = label.Score ?? 0f;
                string labelName = label.Name ?? string.Empty;
                switch (labelName.ToUpperInvariant())
                {
                    case "GRAPHIC": graphic = score; break;
                    case "HATE_SPEECH": hateSpeech = score; break;
                    case "HARASSMENT": harassment = score; break;
                    case "INSULT": insult = score; break;
                    case "PROFANITY": profanity = score; break;
                    case "SEXUAL": sexual = score; break;
                    case "VIOLENCE": violence = score; break;
                }
            }

            // Overall toxicity score provided by AWS
            overall = result.Toxicity ?? 0f;

            return new ToxicityScanResult(
                Overall: overall,
                Graphic: graphic,
                HateSpeech: hateSpeech,
                Harassment: harassment,
                Insult: insult,
                Profanity: profanity,
                Sexual: sexual,
                Violence: violence);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AWS Comprehend toxicity detection failed.");
            return new ToxicityScanResult(0, 0, 0, 0, 0, 0, 0, 0);
        }
    }

    public bool CanHandle(LlmSettings settings)
    {
        return settings?.ToxicityProvider?.Equals("AmazonComprehend", StringComparison.OrdinalIgnoreCase) == true;
    }
}
