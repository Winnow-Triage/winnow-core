using System.Security.Cryptography;
using System.Text;
using Winnow.API.Services.Caching;

namespace Winnow.API.Infrastructure.Security.PoW;

public class PoWValidator(ICacheService cache) : IPoWValidator
{
    public bool Verify(string? apiKey, string method, string path, string timestamp, string nonce, int difficulty)
    {
        if (difficulty <= 0) return true;

        // Hash data: ApiKey + Method.ToUpper() + Path.ToLower() + Timestamp + Nonce
        // We use ToUpper/ToLower to ensure consistency across clients
        var data = $"{apiKey ?? ""}{method.ToUpperInvariant()}{path.ToLowerInvariant()}{timestamp}{nonce}";
        var bytes = Encoding.UTF8.GetBytes(data);
        var hashBytes = SHA256.HashData(bytes);

        // Convert hash to hex string
        var hashString = Convert.ToHexString(hashBytes).ToLowerInvariant();

        // Check for leading zeros
        var target = new string('0', difficulty);
        return hashString.StartsWith(target);
    }

    public async Task<bool> CheckAndMarkNonceUsedAsync(string nonce, TimeSpan ttl)
    {
        var key = $"PoW:Nonce:{nonce}";

        if (await cache.ExistsAsync(key))
        {
            return false; // Already used
        }

        await cache.SetAsync(key, true, ttl);
        return true;
    }

    public async Task<bool> CheckAndMarkNonceUsedPathScopedAsync(string nonce, string scope, TimeSpan ttl)
    {
        var key = $"PoW:Nonce:{nonce}:{scope.ToLowerInvariant()}";

        if (await cache.ExistsAsync(key))
        {
            return false; // Already used for this scope
        }

        await cache.SetAsync(key, true, ttl);
        return true;
    }
}
