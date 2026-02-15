using Amazon.S3;
using Amazon.S3.Model;

namespace Winnow.Server.Services.Storage;

public class S3StorageService : IStorageService
{
    private readonly IAmazonS3 _s3;
    private readonly S3Settings _settings;
    private static readonly TimeSpan UploadUrlExpiry = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan DownloadUrlExpiry = TimeSpan.FromHours(1);

    public S3StorageService(IAmazonS3 s3, S3Settings settings)
    {
        _s3 = s3;
        _settings = settings;
    }

    /// <summary>
    /// The AWS SDK always generates HTTPS presigned URLs regardless of UseHttp config.
    /// This fixes the scheme to match the configured endpoint (e.g. http for MinIO).
    /// </summary>
    private string FixPresignedUrlScheme(string presignedUrl)
    {
        if (_settings.Endpoint.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            && presignedUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return "http://" + presignedUrl["https://".Length..];
        }
        return presignedUrl;
    }

    public async Task<PresignedUploadResult> GenerateUploadUrlAsync(
        Guid orgId, Guid projectId, string fileName, string contentType, CancellationToken ct = default)
    {
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

        var url = _s3.GetPreSignedURL(request);
        return new PresignedUploadResult(FixPresignedUrlScheme(url), objectKey);
    }

    public async Task<string> GenerateDownloadUrlAsync(string key, CancellationToken ct = default)
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

        return FixPresignedUrlScheme(_s3.GetPreSignedURL(request));
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
    public string AccessKey { get; set; } = "minioadmin";
    public string SecretKey { get; set; } = "minioadmin";
    public string Region { get; set; } = "us-east-1";
    public string QuarantineBucket { get; set; } = "winnow-quarantine";
    public string CleanBucket { get; set; } = "winnow-clean";
}
