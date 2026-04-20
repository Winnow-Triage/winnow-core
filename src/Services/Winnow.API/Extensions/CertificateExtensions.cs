using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;

namespace Winnow.API.Extensions;

/// <summary>
/// Extensions for ensuring infrastructure prerequisites like SSL certificates are met at startup.
/// </summary>
public static class CertificateExtensions
{
    private static readonly object _lock = new();

    /// <summary>
    /// Synchronizes the RDS Root CA bundle from a remote URL to the local path specified in the connection string.
    /// This is required for SSL Mode=VerifyFull in AWS RDS environments.
    /// </summary>
    public static void EnsureRdsSslCertificate(this IConfiguration config)
    {
        var certUrl = config["DB_SSL_CERT_URL"];
        var connStr = config.GetConnectionString("Postgres");

        if (string.IsNullOrEmpty(certUrl) || string.IsNullOrEmpty(connStr))
        {
            return;
        }

        // Only attempt synchronization if the connection string explicitly references a Root Certificate file.
        if (!connStr.Contains("Root Certificate=", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var match = Regex.Match(connStr, @"Root Certificate=([^;]+)", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));
        if (!match.Success)
        {
            return;
        }

        var certPath = match.Groups[1].Value.Trim();

        // Lock to prevent race conditions if multiple initialization paths call this simultaneously.
        lock (_lock)
        {
            if (File.Exists(certPath))
            {
                return;
            }

            Console.WriteLine($"[SSL] Initializing RDS SSL certificate synchronization...");
            Console.WriteLine($"[SSL] Source: {certUrl}");
            Console.WriteLine($"[SSL] Target: {certPath}");

            const int maxRetries = 3;
            for (int i = 1; i <= maxRetries; i++)
            {
                try
                {
                    using var client = new HttpClient();
                    client.Timeout = TimeSpan.FromSeconds(30);

                    // Task.Run().GetAwaiter().GetResult() is safer than direct GetResult() on some Task types 
                    // but here in startup it's generally equivalent.
                    var certData = client.GetByteArrayAsync(certUrl).GetAwaiter().GetResult();

                    var directory = Path.GetDirectoryName(certPath);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    File.WriteAllBytes(certPath, certData);
                    Console.WriteLine($"[SSL] Successfully synchronized RDS SSL certificate (Found {certData.Length} bytes).");
                    return;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[SSL] WARNING: Attempt {i}/{maxRetries} failed to download certificate: {ex.Message}");

                    if (i < maxRetries)
                    {
                        // Simple linear backoff before retry
                        Thread.Sleep(2000 * i);
                    }
                    else
                    {
                        Console.Error.WriteLine("[SSL] FATAL: Failed to download RDS SSL certificate after multiple attempts.");
                        Console.Error.WriteLine("[SSL] StackTrace: " + ex.StackTrace);

                        // We do not throw here to allow the application to attempt a standard connection, 
                        // though it will likely fail if SSL Mode=VerifyFull is enforced.
                    }
                }
            }
        }
    }
}
