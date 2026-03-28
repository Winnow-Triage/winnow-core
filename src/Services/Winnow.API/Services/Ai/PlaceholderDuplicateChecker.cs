namespace Winnow.API.Services.Ai;

/// <summary>
/// A non-functional duplicate checker used when no LLM provider is configured.
/// Prevents Dependency Injection failures in CI/CD or light-weight local environments.
/// </summary>
public class PlaceholderDuplicateChecker : IDuplicateChecker
{
    public Task<bool> AreDuplicatesAsync(string titleA, string descA, string titleB, string descB, CancellationToken ct)
    {
        // Always fail-safe to false when AI is disabled
        return Task.FromResult(false);
    }
}
