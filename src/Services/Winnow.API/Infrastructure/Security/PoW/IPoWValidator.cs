namespace Winnow.API.Infrastructure.Security.PoW;

public interface IPoWValidator
{
    bool Verify(string? apiKey, string method, string path, string timestamp, string nonce, int difficulty);
    Task<bool> CheckAndMarkNonceUsedAsync(string nonce, TimeSpan ttl);
}
