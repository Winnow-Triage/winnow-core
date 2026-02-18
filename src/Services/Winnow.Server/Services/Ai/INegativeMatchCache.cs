namespace Winnow.Server.Services.Ai;

public interface INegativeMatchCache
{
    bool IsKnownMismatch(string tenantId, Guid reportA, Guid reportB);
    void MarkAsMismatch(string tenantId, Guid reportA, Guid reportB);
}
