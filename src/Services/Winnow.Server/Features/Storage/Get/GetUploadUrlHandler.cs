using MediatR;
using Winnow.Server.Services.Storage;
using Winnow.Server.Infrastructure.Security.Authorization;
using Winnow.Server.Features.Shared;

namespace Winnow.Server.Features.Storage.Get;

[RequirePermission("reports:write")]
public class GetUploadUrlQuery : IRequest<GetUploadUrlResult>, IOrgScopedRequest
{
    public Guid CurrentOrganizationId { get; set; }
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
            request.CurrentOrganizationId, request.ProjectId, request.FileName, request.ContentType, cancellationToken);

        return new GetUploadUrlResult
        {
            UploadUrl = result.UploadUrl,
            ObjectKey = result.ObjectKey
        };
    }
}
