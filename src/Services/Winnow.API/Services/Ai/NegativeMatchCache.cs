using Winnow.API.Services.Caching;

namespace Winnow.API.Services.Ai;

public class NegativeMatchCache(ICacheService cache) : INegativeMatchCache
{
    public bool IsKnownMismatch(string tenantId, Guid reportA, Guid reportB)
    {
        var key = GetKey(tenantId, reportA, reportB);
        // Using Result here because the original interface is synchronous. 
        // In a real implementation, we should update the interface to be async.
        return cache.GetAsync<bool>(key).GetAwaiter().GetResult();
    }

    public void MarkAsMismatch(string tenantId, Guid reportA, Guid reportB)
    {
        var key = GetKey(tenantId, reportA, reportB);

        // Cache for 24 hours. If we haven't merged them by then, 
        // maybe the reports changed enough to be worth checking again.
        cache.SetAsync(key, true, TimeSpan.FromHours(24)).GetAwaiter().GetResult();
    }

    private string GetKey(string tenantId, Guid a, Guid b)
    {
        // CRITICAL: Always sort IDs so (A, B) and (B, A) produce the same key
        var min = a.CompareTo(b) < 0 ? a : b;
        var max = a.CompareTo(b) < 0 ? b : a;
        return $"NegMatch:{tenantId}:{min}:{max}";
    }
}
