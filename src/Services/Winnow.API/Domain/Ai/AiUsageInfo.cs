namespace Winnow.API.Domain.Ai;

public record AiUsageInfo(int PromptTokens, int CompletionTokens, string ModelId, string Provider);
