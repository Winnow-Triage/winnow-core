using Microsoft.Extensions.Caching.Memory;

namespace Winnow.Server.Services.Ai;

public class NegativeMatchCache(IMemoryCache cache) : INegativeMatchCache
{
    public bool IsKnownMismatch(string tenantId, Guid ticketA, Guid ticketB)
    {
        var key = GetKey(tenantId, ticketA, ticketB);
        return cache.TryGetValue(key, out _);
    }

    public void MarkAsMismatch(string tenantId, Guid ticketA, Guid ticketB)
    {
        var key = GetKey(tenantId, ticketA, ticketB);
        
        // Cache for 24 hours. If we haven't merged them by then, 
        // maybe the tickets changed enough to be worth checking again.
        cache.Set(key, true, TimeSpan.FromHours(24));
    }

    private string GetKey(string tenantId, Guid a, Guid b)
    {
        // CRITICAL: Always sort IDs so (A, B) and (B, A) produce the same key
        var min = a.CompareTo(b) < 0 ? a : b;
        var max = a.CompareTo(b) < 0 ? b : a;
        return $"NegMatch:{tenantId}:{min}:{max}";
    }
}
