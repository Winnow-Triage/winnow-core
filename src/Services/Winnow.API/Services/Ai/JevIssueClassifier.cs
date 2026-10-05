using Microsoft.Extensions.Options;
using Winnow.API.Infrastructure.Configuration;
using Winnow.API.Services.Ai.TypeSafe;

namespace Winnow.API.Services.Ai;

public class JevIssueClassifier(ITypeSafeDecisionClient client, IOptions<LlmSettings> settings, ILogger<JevIssueClassifier> logger) : IIssueClassifier
{
    private readonly LlmSettings _settings = settings.Value;
    public async Task<string> ClassifyAsync(string title, string description, CancellationToken ct = default)
    {
        var state = $@"Title: {title}\nDescription: {description}";
        var request = new JevDecisionRequest
        {
            Model = _settings.TypeSafe.ModelId,
            State = state,
            Questions = new Dictionary<string, object>
            {
                { "issue_type", new { type = "choice", instructions = "Categorize this issue.", choices = new Dictionary<string, string> { { "bug", "A defect, error, or unexpected behavior in the system." }, { "feature_request", "A request for new functionality or an enhancement to existing features." }, { "support_ticket", "A user asking for help, guidance, or documentation." }, { "unknown", "The input is too vague to classify." } } } }
            }
        };
        var response = await client.EvaluateAsync(request, ct);
        if (response?.Answers != null && response.Answers.TryGetValue("issue_type", out var answer))
        {
            var classification = answer.Choice ?? "unknown";
            logger.LogInformation("Jev Classification: {Classification}", classification);
            return classification;
        }
        logger.LogWarning("Jev API returned invalid response for classification. Defaulting to unknown.");
        return "unknown";
    }
}
