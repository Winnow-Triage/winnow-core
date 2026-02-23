using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Winnow.Server.Infrastructure.Security;

/// <summary>
/// EF Core string value converter using AES-256 for transparent encryption.
/// </summary>
public class EncryptedStringConverter : ValueConverter<string?, string?>
{
    public EncryptedStringConverter(string base64MasterKey, ConverterMappingHints? mappingHints = null)
        : base(
            v => v == null ? null : AesEncryptionProvider.Encrypt(v, base64MasterKey),
            v => v == null ? null : AesEncryptionProvider.Decrypt(v, base64MasterKey),
            mappingHints)
    {
    }
}
