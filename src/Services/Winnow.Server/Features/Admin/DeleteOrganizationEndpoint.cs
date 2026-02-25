using Amazon.S3;
using Amazon.S3.Model;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Infrastructure.Persistence;
using Winnow.Server.Services.Storage;

namespace Winnow.Server.Features.Admin;

public class DeleteOrganizationRequest
{
    public Guid Id { get; set; }
}

/// <summary>
/// Admin endpoint to hard delete an organization.
/// Attempts to clean up S3 assets before deleting the database record.
/// </summary>
public sealed class DeleteOrganizationEndpoint(
    WinnowDbContext dbContext,
    IAmazonS3 s3,
    S3Settings s3Settings,
    ILogger<DeleteOrganizationEndpoint> logger) : Endpoint<DeleteOrganizationRequest>
{
    public override void Configure()
    {
        Delete("/admin/organizations/{Id}");
        Roles("SuperAdmin");
        Summary(s =>
        {
            s.Summary = "Hard delete an organization (SuperAdmin only)";
            s.Description = "Permanently deletes an organization and attempts to clean up all associated S3 assets.";
            s.Response(204, "Successfully deleted");
            s.Response(404, "Organization not found");
        });
    }

    public override async Task HandleAsync(DeleteOrganizationRequest req, CancellationToken ct)
    {
        var org = await dbContext.Organizations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(o => o.Id == req.Id, ct);

        if (org == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        // Best effort: Try to clean up S3 buckets for this organization prefix
        var orgPrefix = $"organizations/{req.Id}/";
        await TryDeletePrefixAsync(s3Settings.QuarantineBucket, orgPrefix, ct);
        await TryDeletePrefixAsync(s3Settings.CleanBucket, orgPrefix, ct);

        // Manual cleanup to satisfy Restrict constraints
        // 1. Delete all reports associated with projects in this organization
        var projectIds = await dbContext.Projects
            .Where(p => p.OrganizationId == req.Id)
            .Select(p => p.Id)
            .ToListAsync(ct);

        if (projectIds.Count > 0)
        {
            var reports = await dbContext.Reports
                .Where(r => projectIds.Contains(r.ProjectId))
                .ToListAsync(ct);

            if (reports.Count > 0)
            {
                logger.LogInformation("Cleaning up {Count} reports for organization {OrgId}", reports.Count, req.Id);
                dbContext.Reports.RemoveRange(reports);
                await dbContext.SaveChangesAsync(ct);
            }

            // 2. Delete all projects
            var projects = await dbContext.Projects
                .Where(p => p.OrganizationId == req.Id)
                .ToListAsync(ct);

            logger.LogInformation("Cleaning up {Count} projects for organization {OrgId}", projects.Count, req.Id);
            dbContext.Projects.RemoveRange(projects);
            await dbContext.SaveChangesAsync(ct);
        }

        // 3. Delete the organization from DB (Cascade delete will handle Teams, Members, Invitations)
        dbContext.Organizations.Remove(org);
        await dbContext.SaveChangesAsync(ct);

        await Send.NoContentAsync(ct);
    }

    private async Task TryDeletePrefixAsync(string bucketName, string prefix, CancellationToken ct)
    {
        try
        {
            var listRequest = new ListObjectsV2Request
            {
                BucketName = bucketName,
                Prefix = prefix
            };

            ListObjectsV2Response listResponse;
            do
            {
                listResponse = await s3.ListObjectsV2Async(listRequest, ct);

                if (listResponse.S3Objects.Count > 0)
                {
                    var deleteRequest = new DeleteObjectsRequest
                    {
                        BucketName = bucketName,
                        Objects = [.. listResponse.S3Objects.Select(o => new KeyVersion { Key = o.Key })]
                    };
                    await s3.DeleteObjectsAsync(deleteRequest, ct);
                    logger.LogInformation("Deleted {Count} objects from bucket {Bucket} for prefix {Prefix}", listResponse.S3Objects.Count, bucketName, prefix);
                }

                listRequest.ContinuationToken = listResponse.NextContinuationToken;
            } while (listResponse.IsTruncated == true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to clean up S3 assets for prefix {Prefix} in bucket {Bucket}. Deletion will proceed regardless.", prefix, bucketName);
        }
    }
}
