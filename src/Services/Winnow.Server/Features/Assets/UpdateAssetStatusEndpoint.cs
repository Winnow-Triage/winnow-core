using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Domain.Assets.ValueObjects;
using Winnow.Server.Infrastructure.Persistence;
using Winnow.Server.Infrastructure.Security;

namespace Winnow.Server.Features.Assets;

/// <summary>
/// Request to update an asset's vulnerability scan status.
/// </summary>
public class UpdateAssetStatusRequest
{
    /// <summary>
    /// The original S3 key of the asset.
    /// </summary>
    public string S3Key { get; set; } = default!;

    /// <summary>
    /// New status: Clean, Infected, or Failed.
    /// </summary>
    public string Status { get; set; } = default!;

    /// <summary>
    /// New S3 key if the asset was moved (e.g. from quarantine to clean bucket).
    /// </summary>
    public string? NewS3Key { get; set; }

    /// <summary>
    /// Detected MIME type.
    /// </summary>
    public string? ContentType { get; set; }
}

public sealed class UpdateAssetStatusEndpoint(
    WinnowDbContext dbContext,
    ILogger<UpdateAssetStatusEndpoint> logger) : Endpoint<UpdateAssetStatusRequest>
{
    public override void Configure()
    {
        Post("/assets/status");
        AllowAnonymous(); // Allowed anonymously but secured by ApiKeyAuthPreProcessor (Bouncer Key)
        PreProcessor<ApiKeyAuthPreProcessor<UpdateAssetStatusRequest>>();

        Description(b => b.WithName("UpdateAssetStatus"));
        Summary(s =>
        {
            s.Summary = "Update asset status";
            s.Description = "Internal Callback: Updates the status of an asset after virus scanning.";
            s.Response(204, "Status updated");
            s.Response(400, "Invalid status");
            s.Response(404, "Asset not found");
        });
    }

    public override async Task HandleAsync(UpdateAssetStatusRequest req, CancellationToken ct)
    {
        var asset = await dbContext.Assets.FirstOrDefaultAsync(a => a.S3Key == req.S3Key, ct);
        if (asset == null)
        {
            logger.LogWarning("Asset not found for S3Key: {S3Key}", req.S3Key);
            await Send.NotFoundAsync(ct);
            return;
        }

        AssetStatus newStatus;
        try
        {
            if (!AssetStatus.TryFromName(req.Status, out var status))
            {
                AddError("Invalid status. Must be Clean, Infected, or Failed.");
                await Send.ErrorsAsync(cancellation: ct);
                return;
            }
            newStatus = status!.Value;
        }
        catch (ArgumentException)
        {
            AddError("Invalid status. Must be Clean, Infected, or Failed.");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        if (newStatus == AssetStatus.Pending)
        {
            AddError("Invalid status. Must be Clean, Infected, or Failed.");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        if (newStatus == AssetStatus.Clean)
        {
            if (string.IsNullOrEmpty(req.NewS3Key))
            {
                AddError("NewS3Key is required when status is Clean.");
                await Send.ErrorsAsync(cancellation: ct);
                return;
            }
            asset.MarkAsClean(req.NewS3Key);
        }
        else if (newStatus == AssetStatus.Infected)
        {
            asset.MarkAsInfected();
        }
        else if (newStatus == AssetStatus.Failed)
        {
            asset.MarkAsFailed("Scanning failed");
        }

        if (!string.IsNullOrEmpty(req.ContentType))
        {
            asset.UpdateContentType(req.ContentType);
        }

        await dbContext.SaveChangesAsync(ct);

        logger.LogInformation("Asset {AssetId} status updated to {Status} (S3Key: {S3Key})",
            asset.Id, newStatus, asset.S3Key);

        HttpContext.Response.StatusCode = 204;
    }
}
