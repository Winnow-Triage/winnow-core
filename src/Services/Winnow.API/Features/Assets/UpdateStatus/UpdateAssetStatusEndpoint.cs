using FastEndpoints;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Winnow.API.Domain.Assets.ValueObjects;
using Winnow.API.Infrastructure.Persistence;
using Winnow.API.Infrastructure.Security;

namespace Winnow.API.Features.Assets.UpdateStatus;

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

public sealed class UpdateAssetStatusEndpoint(IMediator mediator) : Endpoint<UpdateAssetStatusRequest>
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
        var command = new UpdateAssetStatusCommand
        {
            S3Key = req.S3Key,
            Status = req.Status,
            NewS3Key = req.NewS3Key,
            ContentType = req.ContentType
        };

        try
        {
            await mediator.Send(command, ct);
            HttpContext.Response.StatusCode = 204;
        }
        catch (InvalidOperationException)
        {
            await Send.NotFoundAsync(ct);
        }
        catch (ArgumentException ex)
        {
            AddError(ex.Message);
            await Send.ErrorsAsync(cancellation: ct);
        }
    }
}
