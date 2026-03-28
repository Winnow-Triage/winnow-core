using FastEndpoints;
using MediatR;
using System.Threading;
using System.Threading.Tasks;
using Winnow.API.Features.Shared;

namespace Winnow.API.Features.Reports.Search;

public class SearchReportsRequest : ProjectScopedRequest
{
    public string? Q { get; set; }
    public int Page { get; set; } = 1;
    public int Size { get; set; } = 20;
    public string[]? Statuses { get; set; }
    public Guid? ClusterId { get; set; }
    public bool? IsOverage { get; set; }
    public bool? IsLocked { get; set; }
    public string? AssignedTo { get; set; }
    public string SortBy { get; set; } = "UpdatedAt";
    public string SortOrder { get; set; } = "Desc";
}

public sealed class SearchReportsEndpoint : ProjectScopedEndpoint<SearchReportsRequest, PaginatedSearchList<ReportSearchDto>>
{
    private readonly IMediator _mediator;

    public SearchReportsEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Get("/reports/search");
        Summary(s =>
        {
            s.Summary = "Search reports";
            s.Description = "Executes a hybrid semantic and keyword search against the project's reports with complex filtering and sorting.";
            s.Response<PaginatedSearchList<ReportSearchDto>>(200, "Paginated list of search results");
            s.Response(400, "Invalid request");
            s.Response(401, "Unauthorized");
        });
        Options(x => x.RequireAuthorization());
    }

    public override async Task HandleAsync(SearchReportsRequest req, CancellationToken ct)
    {
        var query = new SearchReportsQuery(
            req.CurrentOrganizationId,
            req.CurrentProjectId,
            req.Q ?? string.Empty,
            req.Page,
            req.Size,
            req.Statuses,
            req.ClusterId,
            req.IsOverage,
            req.IsLocked,
            req.AssignedTo,
            req.SortBy,
            req.SortOrder
        );
        var result = await _mediator.Send(query, ct);
        await Send.OkAsync(result, ct);
    }
}
