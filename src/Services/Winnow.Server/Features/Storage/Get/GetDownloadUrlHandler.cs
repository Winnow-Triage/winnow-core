using MediatR;
using Winnow.Server.Services.Storage;

namespace Winnow.Server.Features.Storage.Get;

public record GetDownloadUrlQuery : IRequest<Uri>
{
    public string Key { get; init; } = default!;
}

public class GetDownloadUrlHandler(IStorageService storage) : IRequestHandler<GetDownloadUrlQuery, Uri>
{
    public async Task<Uri> Handle(GetDownloadUrlQuery request, CancellationToken cancellationToken)
    {
        return await storage.GenerateDownloadUrlAsync(request.Key, cancellationToken);
    }
}
