using Microsoft.Extensions.Options;
using Winnow.API.Infrastructure.Configuration;
using Winnow.API.Services.Ai.TypeSafe;

namespace Winnow.API.Services.Ai;

public class JevDuplicateChecker(ITypeSafeDecisionClient client, IOptions<LlmSettings> settings, ILogger<JevDuplicateChecker> logger) : IDuplicateChecker
{
    private readonly LlmSettings _settings = settings.Value;
    public async Task<bool> AreDuplicatesAsync(string titleA, string descA, string titleB, string descB, CancellationToken ct)
    {
        var state = $@"Report A:\nTitle: {titleA}\nDescription: {descA}\n\nReport B:\nTitle: {titleB}\nDescription: {descB}";
        var request = new JevDecisionRequest
        {
            Model = _settings.TypeSafe.ModelId,
            State = state,
            Questions = new Dictionary<string, object>
            {
                { "are_duplicates", new { type = "noul", instructions = "Do these two bug reports describe the SAME underlying issue?", true_when = "They share context and root cause (implied).", false_when = "One is a 'UI Glitch' and the other is 'Gameplay Logic', or they are distinct/unrelated issues." } }
            }
        };
        var response = await client.EvaluateAsync(request, ct);
        if (response?.Answers != null && response.Answers.TryGetValue("are_duplicates", out var answer))
        {
            var probability = answer.Noul ?? 0.0;
            var isDuplicate = probability > 0.80;
            logger.LogInformation("Jev Duplicate Check: {IsDuplicate} (Probability: {Probability})", isDuplicate, probability);
            return isDuplicate;
        }
        logger.LogWarning("Jev API returned invalid response. Defaulting to false.");
        return false;
    }
}
