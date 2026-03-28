using Winnow.API.Services.Caching;

namespace Winnow.API.Services.Ai;

public class NegativeMatchCache(ICacheService cache) : INegativeMatchCache
{
    public async Task<bool> IsKnownMismatchAsync(string tenantId, Guid reportA, Guid reportB)
    {
        var key = GetKey(tenantId, reportA, reportB);
        return await cache.GetAsync<bool>(key);
    }

    public async Task MarkAsMismatchAsync(string tenantId, Guid reportA, Guid reportB)
    {
        var key = GetKey(tenantId, reportA, reportB);

        // Cache for 24 hours. If we haven't merged them by then, 
        // maybe the reports changed enough to be worth checking again.
        await cache.SetAsync(key, true, TimeSpan.FromHours(24));
    }

    private string GetKey(string tenantId, Guid a, Guid b)
    {
        // CRITICAL: Always sort IDs so (A, B) and (B, A) produce the same key
        var min = a.CompareTo(b) < 0 ? a : b;
        var max = a.CompareTo(b) < 0 ? b : a;
        return $"NegMatch:{tenantId}:{min}:{max}";
    }
}
