using Amazon.S3;
using Amazon.S3.Model;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Winnow.Server.Infrastructure.Persistence;
using Winnow.Server.Services.Storage;

namespace Winnow.Server.Features.Admin.Organizations.Delete;

public record DeleteOrganizationCommand : IRequest
{
    public Guid Id { get; init; }
}

public class DeleteOrganizationHandler(
    WinnowDbContext dbContext,
    IAmazonS3 s3,
    S3Settings s3Settings,
    ILogger<DeleteOrganizationHandler> logger) : IRequestHandler<DeleteOrganizationCommand>
{
    public async Task Handle(DeleteOrganizationCommand request, CancellationToken cancellationToken)
    {
        var org = await dbContext.Organizations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(o => o.Id == request.Id, cancellationToken);

        if (org == null)
        {
            throw new InvalidOperationException("Organization not found.");
        }

        // Best effort: Try to clean up S3 buckets for this organization prefix
        var orgPrefix = $"organizations/{request.Id}/";
        await TryDeletePrefixAsync(s3Settings.QuarantineBucket, orgPrefix, cancellationToken);
        await TryDeletePrefixAsync(s3Settings.CleanBucket, orgPrefix, cancellationToken);

        // Manual cleanup to satisfy Restrict constraints
        // 1. Delete all reports associated with projects in this organization
        var projectIds = await dbContext.Projects
            .Where(p => p.OrganizationId == request.Id)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        if (projectIds.Count > 0)
        {
            var reports = await dbContext.Reports
                .Where(r => projectIds.Contains(r.ProjectId))
                .ToListAsync(cancellationToken);

            if (reports.Count > 0)
            {
                logger.LogInformation("Cleaning up {Count} reports for organization {OrgId}", reports.Count, request.Id);
                dbContext.Reports.RemoveRange(reports);
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            // 2. Delete all projects
            var projects = await dbContext.Projects
                .Where(p => p.OrganizationId == request.Id)
                .ToListAsync(cancellationToken);

            logger.LogInformation("Cleaning up {Count} projects for organization {OrgId}", projects.Count, request.Id);
            dbContext.Projects.RemoveRange(projects);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        // 3. Delete the organization from DB (Cascade delete will handle Teams, Members, Invitations)
        dbContext.Organizations.Remove(org);
        await dbContext.SaveChangesAsync(cancellationToken);
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
