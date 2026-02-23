using System.Security.Cryptography;
using BCrypt.Net;

namespace Winnow.Server.Infrastructure.Security;

public interface IApiKeyService
{
    string GeneratePlaintextKey(Guid projectId, string prefix = "wm_live_");
    string HashKey(string plaintext);
    bool VerifyKey(string plaintext, string hash);
}
