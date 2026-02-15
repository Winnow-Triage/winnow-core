using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Entities;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Assets;

public class UpdateAssetStatusRequest
{
    public string S3Key { get; set; } = default!;
    public string Status { get; set; } = default!; // "Clean", "Infected", "Failed"
    public string? NewS3Key { get; set; } // New key after Bouncer moves to clean bucket
}

public class UpdateAssetStatusEndpoint(
    WinnowDbContext dbContext,
    ILogger<UpdateAssetStatusEndpoint> logger) : Endpoint<UpdateAssetStatusRequest>
{
    public override void Configure()
    {
        Post("/api/internal/assets/status");
        AllowAnonymous(); // TODO: Lock down with X-Bouncer-Secret header
        Description(b => b.WithName("UpdateAssetStatus"));
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

        await dbContext.SaveChangesAsync(ct);

        logger.LogInformation("Asset {AssetId} status updated to {Status} (S3Key: {S3Key})",
            asset.Id, newStatus, asset.S3Key);

        HttpContext.Response.StatusCode = 204;
    }
}
