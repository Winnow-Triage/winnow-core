using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Entities;
using Winnow.Server.Infrastructure.Persistence;

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
        AllowAnonymous(); // TODO: Lock down with X-Bouncer-Secret header
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

        if (!Enum.TryParse<AssetStatus>(req.Status, ignoreCase: true, out var newStatus)
            || newStatus == AssetStatus.Pending)
        {
            AddError("Invalid status. Must be Clean, Infected, or Failed.");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        asset.Status = newStatus;
        asset.ScannedAt = DateTime.UtcNow;

        if (!string.IsNullOrEmpty(req.NewS3Key))
        {
            asset.S3Key = req.NewS3Key;
        }

        if (!string.IsNullOrEmpty(req.ContentType))
        {
            asset.ContentType = req.ContentType;
        }

        await dbContext.SaveChangesAsync(ct);

        logger.LogInformation("Asset {AssetId} status updated to {Status} (S3Key: {S3Key})",
            asset.Id, newStatus, asset.S3Key);

        HttpContext.Response.StatusCode = 204;
    }
}
