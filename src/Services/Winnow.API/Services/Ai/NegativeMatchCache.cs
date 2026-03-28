using Microsoft.Extensions.Caching.Memory;

namespace Winnow.API.Services.Ai;

public class NegativeMatchCache(IMemoryCache cache) : INegativeMatchCache
{
    public bool IsKnownMismatch(string tenantId, Guid reportA, Guid reportB)
    {
        var key = GetKey(tenantId, reportA, reportB);
        return cache.TryGetValue(key, out _);
    }

    public void MarkAsMismatch(string tenantId, Guid reportA, Guid reportB)
    {
        var key = GetKey(tenantId, reportA, reportB);

        // Cache for 24 hours. If we haven't merged them by then, 
        // maybe the reports changed enough to be worth checking again.
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
