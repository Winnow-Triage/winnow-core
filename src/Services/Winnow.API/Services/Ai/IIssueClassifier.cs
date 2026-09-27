namespace Winnow.API.Services.Ai;

public interface IIssueClassifier
{
    Task<string> ClassifyAsync(string title, string description, CancellationToken ct = default);
}
