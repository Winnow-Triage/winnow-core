using System.Security.Cryptography;
using BCrypt.Net;

namespace Winnow.Server.Infrastructure.Security;

public class ApiKeyService : IApiKeyService
{
    public string GeneratePlaintextKey(Guid projectId, string prefix = "wm_live_")
    {
        // 1. Generate 32 bytes of cryptographically secure randomness
        var randomBytes = new byte[32];
        RandomNumberGenerator.Fill(randomBytes);

        // 2. Format to be URL-safe Base64
        var base64Secret = Convert.ToBase64String(randomBytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');

        // 3. Construct the token: prefix + projectId format "N" (no hyphens) + secret
        return $"{prefix}{projectId:N}_{base64Secret}";
    }

    public string HashKey(string plaintext)
    {
        // EnhancedHashPassword uses SHA-384 pre-hashing to support secrets longer than 72 bytes
        // WorkFactor 11 is a good modern default for production servers.
        return BCrypt.Net.BCrypt.EnhancedHashPassword(plaintext, workFactor: 11);
    }

    public bool VerifyKey(string plaintext, string hash)
    {
        return BCrypt.Net.BCrypt.EnhancedVerify(plaintext, hash);
    }
}
