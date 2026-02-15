using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Entities;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Assets;

public class UpdateAssetStatusRequest
{
    public Guid AssetId { get; set; }
    public string Status { get; set; } = default!; // "Clean", "Infected", "Failed"
    public string? CleanS3Key { get; set; } // New key after Bouncer moves to clean bucket
}

public class UpdateAssetStatusEndpoint(
    WinnowDbContext dbContext,
    IConfiguration config,
    ILogger<UpdateAssetStatusEndpoint> logger) : Endpoint<UpdateAssetStatusRequest>
{
    public override void Configure()
    {
        Put("/api/internal/assets/{AssetId}/status");
        AllowAnonymous(); // Protected by API key check below
        Description(b => b.WithName("UpdateAssetStatus"));
    }

    public override async Task HandleAsync(UpdateAssetStatusRequest req, CancellationToken ct)
    {
        // Verify internal API key
        var expectedKey = config["InternalApiKey"];
        if (!string.IsNullOrEmpty(expectedKey))
        {
            var providedKey = HttpContext.Request.Headers["X-Internal-Key"].FirstOrDefault();
            if (providedKey != expectedKey)
            {
                await Send.UnauthorizedAsync(ct);
                return;
            }
        }

        var asset = await dbContext.Assets.FirstOrDefaultAsync(a => a.Id == req.AssetId, ct);
        if (asset == null)
        {
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

        if (newStatus == AssetStatus.Clean && !string.IsNullOrEmpty(req.CleanS3Key))
        {
            asset.CleanS3Key = req.CleanS3Key;
        }

        await dbContext.SaveChangesAsync(ct);

        logger.LogInformation("Asset {AssetId} status updated to {Status}", asset.Id, newStatus);

        await Send.OkAsync(ct);
    }
}
