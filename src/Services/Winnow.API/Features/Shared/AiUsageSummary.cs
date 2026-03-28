namespace Winnow.API.Features.Shared;

public class AiUsageSummary
{
    public string Model { get; init; } = string.Empty;
    public string Provider { get; init; } = string.Empty;
    public int InputTokens { get; init; }
    public int OutputTokens { get; init; }
    public int CallCount { get; init; }
}
