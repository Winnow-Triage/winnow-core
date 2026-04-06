using Amazon.S3;
using Amazon.S3.Model;

namespace Winnow.API.Services.Storage;

public class S3StorageService(IAmazonS3 s3, S3Settings settings) : IStorageService
{
    private readonly IAmazonS3 _s3 = s3;
    private readonly S3Settings _settings = settings;
    private static readonly TimeSpan UploadUrlExpiry = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan DownloadUrlExpiry = TimeSpan.FromHours(1);

    /// <summary>
    /// The AWS SDK always generates HTTPS presigned URLs regardless of UseHttp config.
    /// This fixes the scheme to match the configured endpoint (e.g. http for MinIO).
    /// </summary>
    private Uri FixPresignedUrlScheme(string presignedUrl)
    {
        if (_settings.Endpoint.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            && presignedUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            var fixedUrl = "http://" + presignedUrl["https://".Length..];
            return new Uri(fixedUrl);
        }
        return new Uri(presignedUrl);
    }

    public Task<PresignedUploadResult> GenerateUploadUrlAsync(
        Guid orgId, Guid projectId, string fileName, string contentType, long? fileSizeBytes = null, CancellationToken ct = default)
    {
        // 1. The Hard Stop
        const long maxBytes = 100 * 1024 * 1024; // 100 MB
        if (fileSizeBytes > maxBytes)
            throw new ArgumentException($"File size {fileSizeBytes} exceeds the maximum limit of 100MB.", nameof(fileSizeBytes));

        // Sanitize the filename to prevent path traversal
        var safeFileName = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(safeFileName))
            throw new ArgumentException("Invalid file name.", nameof(fileName));

        // Enforce strict folder structure
        var objectKey = $"organizations/{orgId}/projects/{projectId}/{Guid.NewGuid():N}_{safeFileName}";

        var request = new GetPreSignedUrlRequest
        {
            BucketName = _settings.QuarantineBucket,
            Key = objectKey,
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.Add(UploadUrlExpiry),
            ContentType = contentType
        };

        // 2. The Cryptographic Lock
        // S3 will reject the upload if the client tries to change this header.
        if (fileSizeBytes.HasValue)
        {
            request.Headers["Content-Length"] = fileSizeBytes.Value.ToString();
        }

        var url = _s3.GetPreSignedURL(request);
        return Task.FromResult(new PresignedUploadResult(FixPresignedUrlScheme(url), objectKey));
    }

    public Task<Uri> GenerateDownloadUrlAsync(string key, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Object key is required.", nameof(key));

        var request = new GetPreSignedUrlRequest
        {
            BucketName = _settings.CleanBucket,
            Key = key,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.Add(DownloadUrlExpiry)
        };

        var url = _s3.GetPreSignedURL(request);
        return Task.FromResult(FixPresignedUrlScheme(url));
    }

    public async Task EnsureBucketsExistAsync(CancellationToken ct = default)
    {
        foreach (var bucket in new[] { _settings.QuarantineBucket, _settings.CleanBucket })
        {
            if (!await Amazon.S3.Util.AmazonS3Util.DoesS3BucketExistV2Async(_s3, bucket))
            {
                await _s3.PutBucketAsync(bucket, ct);
            }
        }
    }

    public async Task<string> UploadFileAsync(
        Guid orgId, Guid projectId, Guid reportId, Stream stream, string fileName, string contentType, string? tenantId = null, CancellationToken ct = default)
    {
        var safeFileName = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(safeFileName))
            throw new ArgumentException("Invalid file name.", nameof(fileName));

        var objectKey = $"organizations/{orgId}/projects/{projectId}/reports/{reportId}/{Guid.NewGuid():N}_{safeFileName}";

        var request = new PutObjectRequest
        {
            BucketName = _settings.QuarantineBucket,
            Key = objectKey,
            InputStream = stream,
            ContentType = contentType
        };
        request.Metadata.Add("org-id", orgId.ToString());
        request.Metadata.Add("project-id", projectId.ToString());
        request.Metadata.Add("report-id", reportId.ToString());
        if (!string.IsNullOrEmpty(tenantId))
            request.Metadata.Add("tenant-id", tenantId);

        await _s3.PutObjectAsync(request, ct);
        return objectKey;
    }
}

public class S3Settings
{
    public string Endpoint { get; set; } = "http://localhost:9000";
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string Region { get; set; } = "us-east-1";
    public string QuarantineBucket { get; set; } = "winnow-quarantine";
    public string CleanBucket { get; set; } = "winnow-clean";
    public bool ForcePathStyle { get; set; } = true;
}
