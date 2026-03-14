using MediatR;
using Winnow.Server.Services.Storage;

namespace Winnow.Server.Features.Storage.Get;

public record GetUploadUrlQuery : IRequest<GetUploadUrlResult>
{
    public Guid OrganizationId { get; init; }
    public Guid ProjectId { get; init; }
    public string FileName { get; init; } = default!;
    public string ContentType { get; init; } = "application/octet-stream";
}

public record GetUploadUrlResult
{
    public Uri UploadUrl { get; init; } = default!;
    public string ObjectKey { get; init; } = string.Empty;
}

public class GetUploadUrlHandler(IStorageService storage) : IRequestHandler<GetUploadUrlQuery, GetUploadUrlResult>
{
    public async Task<GetUploadUrlResult> Handle(GetUploadUrlQuery request, CancellationToken cancellationToken)
    {
        var result = await storage.GenerateUploadUrlAsync(
            request.OrganizationId, request.ProjectId, request.FileName, request.ContentType, cancellationToken);

        return new GetUploadUrlResult
        {
            UploadUrl = result.UploadUrl,
            ObjectKey = result.ObjectKey
        };
    }
}
