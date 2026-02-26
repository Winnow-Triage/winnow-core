using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Winnow.Sdk.DotNet.Core.Models;

namespace Winnow.Sdk.DotNet.Core;

public class WinnowSdkClient : IDisposable
{
    private readonly string _apiKey;
    private readonly WinnowConfig _config;
    
    // Use a single instance of HttpClient to prevent socket exhaustion
    private static readonly HttpClient _httpClient = new HttpClient();
    
    // For System.Text.Json serialization
    private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="WinnowSdkClient"/> class.
    /// </summary>
    /// <param name="apiKey">Your Winnow API key.</param>
    /// <param name="config">Optional configuration settings.</param>
    public WinnowSdkClient(string apiKey, WinnowConfig config = null)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("API key cannot be null or empty.", nameof(apiKey));

        _apiKey = apiKey;
        _config = config ?? new WinnowConfig();
    }

    /// <summary>
    /// Captures and sends a bug report to the Winnow backend. 
    /// Includes a 2-step process for uploading screenshots if provided.
    /// Designed to NEVER crash the application.
    /// </summary>
    /// <param name="payload">The bug report payload containing message, stack trace, metadata, etc.</param>
    /// <param name="screenshotBytes">Raw bytes for a screenshot image/png. Optional.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task SendReportAsync(ReportPayload payload, byte[] screenshotBytes = null)
    {
        if (_config.OfflineMode)
        {
            Console.WriteLine("[Winnow SDK] Offline mode enabled. Skipping report submission.");
            return;
        }

        if (payload == null)
        {
            Console.WriteLine("[Winnow SDK] Payload cannot be null. Skipping.");
            return;
        }

        try
        {
            // Set defaults if not provided
            if (string.IsNullOrWhiteSpace(payload.AppVersion)) payload.AppVersion = "Unknown";
            if (string.IsNullOrWhiteSpace(payload.Platform)) payload.Platform = "Unknown";

            // Step 1: Pre-sign request for screenshot if bytes are provided
            if (screenshotBytes != null && screenshotBytes.Length > 0)
            {
                var presignUrl = $"{_config.BaseUrl.TrimEnd('/')}/api/reports/presign";
                
                var presignReqObj = new { 
                    fileName = $"screenshot_{Guid.NewGuid():N}.png", 
                    contentType = "image/png" 
                };
                
                using var presignReqMessage = new HttpRequestMessage(HttpMethod.Post, presignUrl);
                presignReqMessage.Headers.Add("X-Winnow-Key", _apiKey);
                presignReqMessage.Content = new StringContent(
                    JsonSerializer.Serialize(presignReqObj, _jsonOptions),
                    Encoding.UTF8,
                    "application/json");

                using var presignResponse = await _httpClient.SendAsync(presignReqMessage).ConfigureAwait(false);
                
                if (presignResponse.IsSuccessStatusCode)
                {
                    var responseBody = await presignResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var presignData = JsonSerializer.Deserialize<PresignedUrlResponse>(responseBody, _jsonOptions);

                    if (presignData != null && !string.IsNullOrWhiteSpace(presignData.UploadUrl) && !string.IsNullOrWhiteSpace(presignData.FileKey))
                    {
                        // Step 2: Upload the actual screenshot using PUT to the presigned URL
                        using var uploadReqMessage = new HttpRequestMessage(HttpMethod.Put, presignData.UploadUrl);
                        // Do NOT include auth headers for the S3 pre-signed URL upload
                        var byteContent = new ByteArrayContent(screenshotBytes);
                        byteContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
                        uploadReqMessage.Content = byteContent;

                        using var uploadResponse = await _httpClient.SendAsync(uploadReqMessage).ConfigureAwait(false);

                        if (uploadResponse.IsSuccessStatusCode)
                        {
                            // Step 3: Attach the returned fileKey
                            payload.ScreenshotKey = presignData.FileKey;
                        }
                        else
                        {
                            Console.WriteLine($"[Winnow SDK] Failed to upload screenshot to S3. Status: {uploadResponse.StatusCode}");
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"[Winnow SDK] Failed to acquire presigned URL. Status: {presignResponse.StatusCode}");
                }
            }

            // Step 4: Submit the actual bug report
            var reportUrl = $"{_config.BaseUrl.TrimEnd('/')}/api/reports";
            using var reportReqMessage = new HttpRequestMessage(HttpMethod.Post, reportUrl);
            reportReqMessage.Headers.Add("X-Winnow-Key", _apiKey);
            
            var payloadJson = JsonSerializer.Serialize(payload, _jsonOptions);
            reportReqMessage.Content = new StringContent(payloadJson, Encoding.UTF8, "application/json");

            using var reportResponse = await _httpClient.SendAsync(reportReqMessage).ConfigureAwait(false);

            if (!reportResponse.IsSuccessStatusCode)
            {
                Console.WriteLine($"[Winnow SDK] Failed to send bug report. Status: {reportResponse.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            // Fail silently or log locally to NEVER crash game engine
            Console.WriteLine($"[Winnow SDK] Exception occurred while sending report: {ex.Message}");
        }
    }

    public void Dispose()
    {
        // Don't dispose the static HttpClient
    }
}
