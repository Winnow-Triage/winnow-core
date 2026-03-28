using MediatR;
using Winnow.API.Services.Storage;
using Winnow.API.Infrastructure.Security.Authorization;
using Winnow.API.Features.Shared;

namespace Winnow.API.Features.Storage.Get;

[RequirePermission("reports:read")]
public class GetDownloadUrlQuery : IRequest<Uri>, IOrgScopedRequest
{
    public string Key { get; init; } = default!;
    public Guid CurrentOrganizationId { get; set; }
}

public class GetDownloadUrlHandler(IStorageService storage) : IRequestHandler<GetDownloadUrlQuery, Uri>
{
    public async Task<Uri> Handle(GetDownloadUrlQuery request, CancellationToken cancellationToken)
    {
        return await storage.GenerateDownloadUrlAsync(request.Key, cancellationToken);
    }
}
