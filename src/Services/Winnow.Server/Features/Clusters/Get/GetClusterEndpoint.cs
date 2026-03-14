using System.Security.Claims;
using FastEndpoints;
using MediatR;

namespace Winnow.Server.Features.Clusters.Get;

public class GetClusterRequest
{
    public Guid Id { get; set; }

    // FastEndpoints magic: Let it bind and validate the header automatically!
    [FromHeader("X-Project-ID", IsRequired = true)]
    public Guid ProjectId { get; set; }
}

public class GetClusterResponse
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string? Title { get; set; }
    public string? Summary { get; set; }
    public int? CriticalityScore { get; set; }
    public string? CriticalityReasoning { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int ReportCount { get; set; }
    public DateTime? FirstSeen { get; set; }
    public DateTime? LastSeen { get; set; }
    public string? AssignedTo { get; set; }
    public int Velocity1h { get; set; }
    public int Velocity24h { get; set; }
    public List<ClusterMemberDto> Reports { get; set; } = [];
}

public class ClusterMemberDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public double? ConfidenceScore { get; set; }
}

public sealed class GetClusterEndpoint(IMediator mediator) : Endpoint<GetClusterRequest, GetClusterResponse>
{
    public override void Configure()
    {
        Get("/clusters/{id:guid}");
        Options(x => x.RequireAuthorization());
    }

    public override async Task HandleAsync(GetClusterRequest req, CancellationToken ct)
    {
        var query = new GetClusterQuery(req.Id, req.ProjectId);
        var result = await mediator.Send(query, ct);

        if (!result.IsSuccess)
        {
            if (result.StatusCode == 404)
            {
                await Send.NotFoundAsync(ct);
                return;
            }

            ThrowError(result.ErrorMessage ?? "Internal Server Error", result.StatusCode ?? 500);
            return;
        }

        await Send.OkAsync(result.Data!, ct);
    }
}