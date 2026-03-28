using System.Security.Claims;
using FastEndpoints;
using MediatR;
using Winnow.API.Features.Shared;

namespace Winnow.API.Features.Clusters.List;

public class ListClustersRequest : ProjectScopedRequest { }

/// <summary>
/// Summary of a cluster.
/// </summary>
public record ClusterDto(
    Guid Id,
    string? Title,
    string? Summary,
    int? CriticalityScore,
    string Status,
    DateTime CreatedAt,
    int ReportCount,
    bool IsLocked,
    bool IsOverage);

public sealed class ListClustersEndpoint(IMediator mediator) : ProjectScopedEndpoint<ListClustersRequest, List<ClusterDto>>
{
    public override void Configure()
    {
        Get("/clusters");
        Summary(s =>
        {
            s.Summary = "List active clusters";
            s.Description = "Retrieves a list of report clusters for the project, including AI summaries and criticality.";
            s.Response<List<ClusterDto>>(200, "List of clusters");
            s.Response(400, "Invalid request");
            s.Response(401, "Unauthorized");
        });
        Options(x => x.RequireAuthorization());
    }

    public override async Task HandleAsync(ListClustersRequest req, CancellationToken ct)
    {
        // Sort parameter
        string sort = HttpContext.Request.Query["sort"].ToString();
        if (string.IsNullOrEmpty(sort)) sort = "criticality";

        var query = new ListClustersQuery(req.CurrentOrganizationId, req.CurrentProjectId, sort);
        var result = await mediator.Send(query, ct);

        if (!result.IsSuccess)
        {
            ThrowError(result.ErrorMessage ?? "Internal Server Error", result.StatusCode ?? 500);
            return;
        }

        await Send.OkAsync(result.Data!, ct);
    }
}
