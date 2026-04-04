using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace Winnow.API.Services.Caching;

public class DistributedCacheService(IDistributedCache cache) : ICacheService
{
    public async Task<T?> GetAsync<T>(string key)
    {
        var data = await cache.GetStringAsync(key);
        if (data == null) return default;
        return JsonSerializer.Deserialize<T>(data);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? ttl = null)
    {
        var options = new DistributedCacheEntryOptions();
        if (ttl.HasValue)
        {
            options.AbsoluteExpirationRelativeToNow = ttl.Value;
        }

        var data = JsonSerializer.Serialize(value);
        await cache.SetStringAsync(key, data, options);
    }

    public async Task<bool> ExistsAsync(string key)
    {
        var data = await cache.GetAsync(key);
        return data != null;
    }

    public async Task RemoveAsync(string key)
    {
        await cache.RemoveAsync(key);
    }
}
