namespace Winnow.API.Services.Ai.TypeSafe;

public interface ITypeSafeDecisionClient
{
    Task<JevDecisionResponse?> EvaluateAsync(JevDecisionRequest request, CancellationToken cancellationToken = default);
}
