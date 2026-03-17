using Amazon.S3;
using Amazon.S3.Model;
using MediatR;
using Winnow.Server.Infrastructure.Security.Authorization;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Features.Shared;
using Winnow.Server.Infrastructure.Persistence;
using Winnow.Server.Services.Storage;

namespace Winnow.Server.Features.Projects.Delete;

[RequirePermission("projects:manage")]
public record DeleteProjectCommand : IRequest, IProjectScopedRequest
{
    public Guid CurrentProjectId { get; set; }
    public Guid CurrentOrganizationId { get; set; }
    public string CurrentUserId { get; set; } = string.Empty;
    public HashSet<string> CurrentUserRoles { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public class DeleteProjectHandler(
    WinnowDbContext dbContext,
    IAmazonS3 s3,
    S3Settings s3Settings,
    ILogger<DeleteProjectHandler> logger) : IRequestHandler<DeleteProjectCommand>
{
    public async Task Handle(DeleteProjectCommand request, CancellationToken ct)
    {
        var project = await dbContext.Projects
            .FirstOrDefaultAsync(p => p.Id == request.CurrentProjectId && p.OrganizationId == request.CurrentOrganizationId, ct);

        if (project == null)
        {
            throw new InvalidOperationException("Project not found.");
        }

        // Check if user is the project owner OR an admin in the organization
        var membership = await dbContext.OrganizationMembers
            .FirstOrDefaultAsync(om => om.OrganizationId == request.CurrentOrganizationId && om.UserId == request.CurrentUserId, ct);

        var isAdmin = membership?.Role.Name == "Admin" || request.HasAnyRole("Admin", "SuperAdmin");
        var isOwner = project.OwnerId == request.CurrentUserId;

        if (!isAdmin && !isOwner)
        {
            throw new UnauthorizedAccessException("Access denied.");
        }

        // Clean up S3 assets for this project
        var projectPrefix = $"organizations/{request.CurrentOrganizationId}/projects/{request.CurrentProjectId}/";
        await TryDeletePrefixAsync(s3Settings.QuarantineBucket, projectPrefix, ct);
        await TryDeletePrefixAsync(s3Settings.CleanBucket, projectPrefix, ct);

        // Manual cascade to satisfy Restrict constraints
        logger.LogInformation("Cleaning up relations for project {ProjectId}", request.CurrentProjectId);

        await dbContext.Assets.Where(a => a.ProjectId == request.CurrentProjectId).ExecuteDeleteAsync(ct);
        await dbContext.Reports.Where(r => r.ProjectId == request.CurrentProjectId).ExecuteDeleteAsync(ct);
        await dbContext.Integrations.Where(i => i.ProjectId == request.CurrentProjectId).ExecuteDeleteAsync(ct);
        await dbContext.ProjectMembers.Where(pm => pm.ProjectId == request.CurrentProjectId).ExecuteDeleteAsync(ct);

        dbContext.Projects.Remove(project);
        await dbContext.SaveChangesAsync(ct);
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
