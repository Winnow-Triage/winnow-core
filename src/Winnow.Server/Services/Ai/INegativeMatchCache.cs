namespace Winnow.Server.Services.Ai;

public interface INegativeMatchCache
{
    bool IsKnownMismatch(string tenantId, Guid ticketA, Guid ticketB);
    void MarkAsMismatch(string tenantId, Guid ticketA, Guid ticketB);
}
