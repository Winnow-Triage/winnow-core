using FastEndpoints;
using Winnow.Server.Services.Storage;

namespace Winnow.Server.Features.Storage;

public class GetUploadUrlRequest
{
    public Guid OrganizationId { get; set; }
    public Guid ProjectId { get; set; }
    public string FileName { get; set; } = default!;
    public string ContentType { get; set; } = "application/octet-stream";
}
public class GetUploadUrlResponse
{
    public Uri UploadUrl { get; set; } = default!;
    public string ObjectKey { get; set; } = string.Empty;
}

public sealed class GetUploadUrlEndpoint(IStorageService storage) : Endpoint<GetUploadUrlRequest, GetUploadUrlResponse>
{
    public override void Configure()
    {
        Post("/storage/upload-url");
        Description(x => x.WithName("GetUploadUrl"));
    }

    public override async Task HandleAsync(GetUploadUrlRequest req, CancellationToken ct)
    {
        var result = await storage.GenerateUploadUrlAsync(
            req.OrganizationId, req.ProjectId, req.FileName, req.ContentType, ct);

        await Send.OkAsync(new GetUploadUrlResponse
        {
            UploadUrl = result.UploadUrl,
            ObjectKey = result.ObjectKey
        }, ct);
    }
}
public class GetDownloadUrlRequest
{
    public string Key { get; set; } = default!;
}
public class GetDownloadUrlResponse
{
    public Uri DownloadUrl { get; set; } = default!;
}

public sealed class GetDownloadUrlEndpoint(IStorageService storage) : Endpoint<GetDownloadUrlRequest, GetDownloadUrlResponse>
{
    public override void Configure()
    {
        Post("/storage/download-url");
        Description(x => x.WithName("GetDownloadUrl"));
    }

    public override async Task HandleAsync(GetDownloadUrlRequest req, CancellationToken ct)
    {
        var url = await storage.GenerateDownloadUrlAsync(req.Key, ct);

        await Send.OkAsync(new GetDownloadUrlResponse
        {
            DownloadUrl = url
        }, ct);
    }
}
