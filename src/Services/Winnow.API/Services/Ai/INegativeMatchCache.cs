namespace Winnow.API.Services.Ai;

public interface INegativeMatchCache
{
    Task<bool> IsKnownMismatchAsync(string tenantId, Guid reportA, Guid reportB);
    Task MarkAsMismatchAsync(string tenantId, Guid reportA, Guid reportB);
}
