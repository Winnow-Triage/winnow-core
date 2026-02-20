using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Winnow.Server.Services.Storage;

namespace Winnow.Server.Infrastructure.HealthChecks;

public class S3StorageHealthCheck : IHealthCheck
{
    private readonly IAmazonS3 _s3Client;
    private readonly S3Settings _s3Settings;
    
    public S3StorageHealthCheck(IAmazonS3 s3Client, S3Settings s3Settings)
    {
        _s3Client = s3Client;
        _s3Settings = s3Settings;
    }
    
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        var data = new Dictionary<string, object>
        {
            ["Endpoint"] = _s3Settings.Endpoint,
            ["QuarantineBucket"] = _s3Settings.QuarantineBucket,
            ["CleanBucket"] = _s3Settings.CleanBucket,
            ["Region"] = _s3Settings.Region
        };
        
        var failures = new List<string>();
        
        // Test S3 connection by listing buckets
        try
        {
            var listBucketsResponse = await _s3Client.ListBucketsAsync(cancellationToken);
            data["Connection"] = "Successful";
            data["BucketCount"] = listBucketsResponse.Buckets.Count;
        }
        catch (Exception ex)
        {
            data["Connection"] = $"Failed: {ex.Message}";
            failures.Add($"S3 Connection: {ex.Message}");
        }
        
        // Check if required buckets exist
        foreach (var bucket in new[] { _s3Settings.QuarantineBucket, _s3Settings.CleanBucket })
        {
            try
            {
                var bucketExists = await Amazon.S3.Util.AmazonS3Util.DoesS3BucketExistV2Async(_s3Client, bucket);
                data[$"BucketExists_{bucket}"] = bucketExists;
                
                if (!bucketExists)
                {
                    failures.Add($"Bucket '{bucket}' does not exist");
                }
                else
                {
                    // Try a simple operation to verify bucket access
                    try
                    {
                        var request = new ListObjectsV2Request
                        {
                            BucketName = bucket,
                            MaxKeys = 1
                        };
                        await _s3Client.ListObjectsV2Async(request, cancellationToken);
                        data[$"BucketAccess_{bucket}"] = "Healthy";
                    }
                    catch (Exception ex)
                    {
                        data[$"BucketAccess_{bucket}"] = $"Limited: {ex.Message}";
                        // Not adding to failures as bucket exists but may have permission issues
                    }
                }
            }
            catch (Exception ex)
            {
                data[$"BucketCheck_{bucket}"] = $"Error: {ex.Message}";
                failures.Add($"Bucket '{bucket}' check failed: {ex.Message}");
            }
        }
        
        if (failures.Count == 0)
        {
            return HealthCheckResult.Healthy(
                "S3/MinIO storage is healthy and accessible", 
                data);
        }
        
        return HealthCheckResult.Unhealthy(
            $"S3/MinIO storage has {failures.Count} issue(s)", 
            data: data);
    }
}