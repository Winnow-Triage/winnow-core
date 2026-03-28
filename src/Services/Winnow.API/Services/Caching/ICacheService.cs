namespace Winnow.API.Services.Caching;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan? ttl = null);
    Task<bool> ExistsAsync(string key);
    Task RemoveAsync(string key);
}
