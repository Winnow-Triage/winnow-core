namespace Winnow.Server.Services.Storage;

public interface IStorageService
{
    /// <summary>
    /// Generates a presigned PUT URL for uploading a file to the quarantine bucket.
    /// Path is enforced: organizations/{orgId}/projects/{projectId}/{fileName}
    /// </summary>
    Task<PresignedUploadResult> GenerateUploadUrlAsync(
        Guid orgId, Guid projectId, string fileName, string contentType, CancellationToken ct = default);

    /// <summary>
    /// Generates a presigned GET URL for downloading a processed file from the clean bucket.
    /// </summary>
    Task<string> GenerateDownloadUrlAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// Ensures the quarantine and clean buckets exist, creating them if necessary.
    /// </summary>
    Task EnsureBucketsExistAsync(CancellationToken ct = default);

    /// <summary>
    /// Uploads a file directly to the quarantine bucket from server-side code.
    /// Returns the object key.
    /// </summary>
    Task<string> UploadFileAsync(
        Guid orgId, Guid projectId, Guid reportId, Stream stream, string fileName, string contentType, CancellationToken ct = default);
}

public record PresignedUploadResult(string UploadUrl, string ObjectKey);
