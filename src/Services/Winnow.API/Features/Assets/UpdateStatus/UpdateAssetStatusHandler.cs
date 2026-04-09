using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Winnow.API.Domain.Assets.ValueObjects;
using Winnow.API.Infrastructure.Persistence;

namespace Winnow.API.Features.Assets.UpdateStatus;

public record UpdateAssetStatusCommand : IRequest
{
    public string S3Key { get; init; } = default!;
    public string Status { get; init; } = default!;
    public string? NewS3Key { get; init; }
    public string? ContentType { get; init; }
}

public class UpdateAssetStatusHandler(
    WinnowDbContext dbContext,
    ILogger<UpdateAssetStatusHandler> logger) : IRequestHandler<UpdateAssetStatusCommand>
{
    public async Task Handle(UpdateAssetStatusCommand request, CancellationToken cancellationToken)
    {
        var asset = await dbContext.Assets
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(a => a.S3Key == request.S3Key, cancellationToken);
        if (asset == null)
        {
            logger.LogWarning("Asset not found for S3Key: {S3Key}", request.S3Key);
            throw new InvalidOperationException("Asset not found");
        }

        AssetStatus newStatus;
        try
        {
            if (!AssetStatus.TryFromName(request.Status, out var status))
            {
                throw new ArgumentException("Invalid status. Must be Clean, Infected, or Failed.");
            }
            newStatus = status!.Value;
        }
        catch (ArgumentException)
        {
            throw new ArgumentException("Invalid status. Must be Clean, Infected, or Failed.");
        }

        if (newStatus == AssetStatus.Pending)
        {
            throw new ArgumentException("Invalid status. Must be Clean, Infected, or Failed.");
        }

        if (newStatus == AssetStatus.Clean)
        {
            if (string.IsNullOrEmpty(request.NewS3Key))
            {
                throw new ArgumentException("NewS3Key is required when status is Clean.");
            }
            asset.MarkAsClean(request.NewS3Key);
        }
        else if (newStatus == AssetStatus.Infected)
        {
            asset.MarkAsInfected();
        }
        else if (newStatus == AssetStatus.Failed)
        {
            asset.MarkAsFailed("Scanning failed");
        }

        if (!string.IsNullOrEmpty(request.ContentType))
        {
            asset.UpdateContentType(request.ContentType);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Asset {AssetId} status updated to {Status} (S3Key: {S3Key})",
            asset.Id, newStatus, asset.S3Key);
    }
}
