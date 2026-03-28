using System.Security.Cryptography;

namespace Winnow.API.Infrastructure.Security;

/// <summary>
/// Provides application-level AES-256 encryption.
/// </summary>
public static class AesEncryptionProvider
{
    private const int KeySizeInBits = 256;
    private const int IvSizeInBytes = 16; // AES block size is 128-bit (16 bytes)

    public static string? Encrypt(string? plainText, string base64MasterKey)
    {
        if (string.IsNullOrEmpty(plainText))
            return plainText;

        byte[] keyBytes = Convert.FromBase64String(base64MasterKey);
        if (keyBytes.Length * 8 != KeySizeInBits)
            throw new ArgumentException($"Master key must be exactly {KeySizeInBits} bits.");

        using var aes = Aes.Create();
        aes.KeySize = KeySizeInBits;
        aes.Key = keyBytes;
        aes.GenerateIV(); // Generates a random 16-byte IV for every encryption

        using var encryptor = aes.CreateEncryptor();
        using var ms = new MemoryStream();

        // Write the randomly generated IV into the beginning of the stream
        ms.Write(aes.IV, 0, aes.IV.Length);

        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        using (var sw = new StreamWriter(cs))
        {
            sw.Write(plainText);
        }

        // Return the combined array: IV + CipherText as a Base64 string
        return Convert.ToBase64String(ms.ToArray());
    }

    public static string? Decrypt(string? cipherText, string base64MasterKey)
    {
        if (string.IsNullOrEmpty(cipherText))
            return cipherText;

        byte[] fullCipher = Convert.FromBase64String(cipherText);

        // Ciphertext must contain at minimum the 16 bytes for the IV
        if (fullCipher.Length < IvSizeInBytes)
            throw new FormatException("Invalid ciphertext. payload does not contain a valid IV.");

        byte[] keyBytes = Convert.FromBase64String(base64MasterKey);
        if (keyBytes.Length * 8 != KeySizeInBits)
            throw new ArgumentException($"Master key must be exactly {KeySizeInBits} bits.");

        // Extract the IV directly from the start of the payload
        byte[] iv = new byte[IvSizeInBytes];
        Buffer.BlockCopy(fullCipher, 0, iv, 0, IvSizeInBytes);

        using var aes = Aes.Create();
        aes.KeySize = KeySizeInBits;
        aes.Key = keyBytes;
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        // Feed in only the actual encrypted data (skipping the IV bytes)
        using var ms = new MemoryStream(fullCipher, IvSizeInBytes, fullCipher.Length - IvSizeInBytes);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var sr = new StreamReader(cs);

        return sr.ReadToEnd();
    }
}
