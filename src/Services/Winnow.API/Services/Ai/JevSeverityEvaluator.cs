using Microsoft.Extensions.Options;
using Winnow.API.Infrastructure.Configuration;
using Winnow.API.Services.Ai.TypeSafe;

namespace Winnow.API.Services.Ai;

public class JevSeverityEvaluator(ITypeSafeDecisionClient client, IOptions<LlmSettings> settings, ILogger<JevSeverityEvaluator> logger) : ISeverityEvaluator
{
    private readonly LlmSettings _settings = settings.Value;
    public async Task<int> EvaluateSeverityAsync(string title, string description, CancellationToken ct = default)
    {
        var state = $@"Title: {title}\nDescription: {description}";
        var request = new JevDecisionRequest
        {
            Model = _settings.TypeSafe.ModelId,
            State = state,
            Questions = new Dictionary<string, object>
            {
                { "severity", new { type = "score", instructions = "Rate the severity of this issue on a scale from 1 to 10.", min = 1.0, max = 10.0, min_label = "Trivial, purely cosmetic, or minor annoyance with an easy workaround.", max_label = "Critical system crash, complete data loss, or major security vulnerability." } }
            }
        };
        var response = await client.EvaluateAsync(request, ct);
        if (response?.Answers != null && response.Answers.TryGetValue("severity", out var answer))
        {
            var rawScore = answer.Score ?? 1.0;
            var severity = (int)Math.Round(rawScore);
            severity = Math.Max(1, Math.Min(10, severity));
            logger.LogInformation("Jev Severity Score: {Severity} (Raw: {RawScore})", severity, rawScore);
            return severity;
        }
        logger.LogWarning("Jev API returned invalid response for severity. Defaulting to 1.");
        return 1;
    }
}
