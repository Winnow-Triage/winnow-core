using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Features.Shared;
using Winnow.Server.Infrastructure.Persistence;
using Winnow.Server.Services.Storage;

namespace Winnow.Server.Features.Projects.Delete;

public class DeleteProjectRequest : ProjectScopedRequest { }

public sealed class DeleteProjectEndpoint(
    WinnowDbContext dbContext,
    IAmazonS3 s3,
    S3Settings s3Settings,
    ILogger<DeleteProjectEndpoint> logger)
    : ProjectScopedEndpoint<DeleteProjectRequest>
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

    public override async Task HandleAsync(DeleteProjectRequest req, CancellationToken ct)
    {
        var project = await dbContext.Projects
            .FirstOrDefaultAsync(p => p.Id == req.CurrentProjectId && p.OrganizationId == req.CurrentOrganizationId, ct);

        if (project == null)
        {
            await Send.NotFoundAsync(cancellation: ct);
            return;
        }

        // Check if user is the project owner OR an admin in the organization
        var membership = await dbContext.OrganizationMembers
            .FirstOrDefaultAsync(om => om.OrganizationId == req.CurrentOrganizationId && om.UserId == req.CurrentUserId, ct);

        var isAdmin = membership?.IsAdmin() ?? false;
        var isOwner = project.OwnerId == req.CurrentUserId;

        if (!isAdmin && !isOwner)
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        // Clean up S3 assets for this project
        var projectPrefix = $"organizations/{req.CurrentOrganizationId}/projects/{req.CurrentProjectId}/";
        await TryDeletePrefixAsync(s3Settings.QuarantineBucket, projectPrefix, ct);
        await TryDeletePrefixAsync(s3Settings.CleanBucket, projectPrefix, ct);

        // Manual cascade to satisfy Restrict constraints
        logger.LogInformation("Cleaning up relations for project {ProjectId}", req.CurrentProjectId);

        await dbContext.Assets.Where(a => a.ProjectId == req.CurrentProjectId).ExecuteDeleteAsync(ct);
        await dbContext.Reports.Where(r => r.ProjectId == req.CurrentProjectId).ExecuteDeleteAsync(ct);
        await dbContext.Integrations.Where(i => i.ProjectId == req.CurrentProjectId).ExecuteDeleteAsync(ct);
        await dbContext.ProjectMembers.Where(pm => pm.ProjectId == req.CurrentProjectId).ExecuteDeleteAsync(ct);

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
