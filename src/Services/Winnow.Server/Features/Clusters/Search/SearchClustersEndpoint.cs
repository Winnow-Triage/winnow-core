using MediatR;
using Winnow.Server.Features.Shared;

namespace Winnow.Server.Features.Clusters.Search;

public class SearchClustersRequest : ProjectScopedRequest
{
    public string? Q { get; set; }
    public int Page { get; set; } = 1;
    public int Size { get; set; } = 20;
}

public sealed class SearchClustersEndpoint(IMediator mediator) : ProjectScopedEndpoint<SearchClustersRequest, PaginatedClusterSearchList<ClusterSearchDto>>
{
    private readonly IMediator _mediator = mediator;

    public override void Configure()
    {
        Get("/clusters/search");
        Summary(s =>
        {
            s.Summary = "Search clusters";
            s.Description = "Executes a hybrid semantic and keyword search against the project's clusters.";
            s.Response<PaginatedClusterSearchList<ClusterSearchDto>>(200, "Paginated list of search results");
            s.Response(400, "Invalid request");
            s.Response(401, "Unauthorized");
        });
        Options(x => x.RequireAuthorization());
    }

    public override async Task HandleAsync(SearchClustersRequest req, CancellationToken ct)
    {
        var query = new SearchClustersQuery(req.CurrentProjectId, req.Q ?? string.Empty, req.Page, req.Size);
        var result = await _mediator.Send(query, ct);

        await Send.OkAsync(result, ct);
    }
}
