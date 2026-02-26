using System.Security.Claims;
using Amazon.S3;
using Amazon.S3.Model;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Infrastructure.MultiTenancy;
using Winnow.Server.Infrastructure.Persistence;
using Winnow.Server.Services.Storage;

namespace Winnow.Server.Features.Projects;

public sealed class DeleteProjectEndpoint(
    WinnowDbContext dbContext,
    ITenantContext tenantContext,
    IAmazonS3 s3,
    S3Settings s3Settings,
    ILogger<DeleteProjectEndpoint> logger)
    : EndpointWithoutRequest
{
    public override void Configure()
    {
        Delete("/projects/{ProjectId}");
        Summary(s =>
        {
            s.Summary = "Delete Project";
            s.Description = "Permanently deletes a project, including all of its error reports and generated API keys.";
            s.Response(204, "Project deleted successfully");
            s.Response(400, "Invalid Request");
            s.Response(404, "Project Not Found");
        });
        Options(x => x.RequireAuthorization());
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userId is null) ThrowError("Unauthorized", 401);

        if (!tenantContext.CurrentOrganizationId.HasValue)
        {
            ThrowError("No organization selected", 400);
            return;
        }

        var projectIdStr = Route<string>("ProjectId");
        if (!Guid.TryParse(projectIdStr, out var projectId))
        {
            ThrowError("Invalid Project ID", 400);
            return;
        }

        var project = await dbContext.Projects
            .FirstOrDefaultAsync(p => p.Id == projectId && p.OrganizationId == tenantContext.CurrentOrganizationId.Value, ct);

        if (project == null)
        {
            await Send.NotFoundAsync(cancellation: ct);
            return;
        }

        // Check if user is the project owner OR an admin in the organization
        var membership = await dbContext.OrganizationMembers
            .FirstOrDefaultAsync(om => om.OrganizationId == tenantContext.CurrentOrganizationId.Value && om.UserId == userId, ct);

        var isAdmin = membership?.IsAdmin() ?? false;
        var isOwner = project.OwnerId == userId;

        if (!isAdmin && !isOwner)
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        // Clean up S3 assets for this project
        var projectPrefix = $"organizations/{tenantContext.CurrentOrganizationId.Value}/projects/{projectId}/";
        await TryDeletePrefixAsync(s3Settings.QuarantineBucket, projectPrefix, ct);
        await TryDeletePrefixAsync(s3Settings.CleanBucket, projectPrefix, ct);

        // Manual cascade to satisfy Restrict constraints
        logger.LogInformation("Cleaning up relations for project {ProjectId}", projectId);

        await dbContext.Assets.Where(a => a.ProjectId == projectId).ExecuteDeleteAsync(ct);
        await dbContext.Reports.Where(r => r.ProjectId == projectId).ExecuteDeleteAsync(ct);
        await dbContext.Integrations.Where(i => i.ProjectId == projectId).ExecuteDeleteAsync(ct);
        await dbContext.ProjectMembers.Where(pm => pm.ProjectId == projectId).ExecuteDeleteAsync(ct);

        dbContext.Projects.Remove(project);

        await dbContext.SaveChangesAsync(ct);

        await Send.NoContentAsync(cancellation: ct);
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
