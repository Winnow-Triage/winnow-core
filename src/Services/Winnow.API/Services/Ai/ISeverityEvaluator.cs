namespace Winnow.API.Services.Ai;

public interface ISeverityEvaluator
{
    Task<int> EvaluateSeverityAsync(string title, string description, CancellationToken ct = default);
}
