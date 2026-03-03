using System.Security.Claims;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Entities;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Clusters.Get;

public class GetClusterRequest
{
    public Guid Id { get; set; }
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
    public List<ClusterMemberDto> Reports { get; set; } = [];
}

public class ClusterMemberDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public float? ConfidenceScore { get; set; }
}

public sealed class GetClusterEndpoint(WinnowDbContext db) : Endpoint<GetClusterRequest, GetClusterResponse>
{
    public override void Configure()
    {
        Get("/clusters/{id:guid}");
        Options(x => x.RequireAuthorization());
    }

    public override async Task HandleAsync(GetClusterRequest req, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) ThrowError("Unauthorized", 401);

        Guid projectId = Guid.Empty;
        if (!HttpContext.Request.Headers.TryGetValue("X-Project-ID", out var projectIdHeader) ||
            !Guid.TryParse(projectIdHeader, out projectId))
        {
            ThrowError("Valid Project ID is required in X-Project-ID header", 400);
        }

        var cluster = await db.Clusters
            .AsNoTracking()
            .Include(c => c.Reports)
            .FirstOrDefaultAsync(c => c.Id == req.Id && c.ProjectId == projectId, ct);

        if (cluster == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(new GetClusterResponse
        {
            Id = cluster.Id,
            ProjectId = cluster.ProjectId,
            Title = cluster.Title,
            Summary = cluster.Summary,
            CriticalityScore = cluster.CriticalityScore,
            CriticalityReasoning = cluster.CriticalityReasoning,
            Status = cluster.Status,
            CreatedAt = cluster.CreatedAt,
            Reports = cluster.Reports
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new ClusterMemberDto
                {
                    Id = r.Id,
                    Title = r.Title,
                    Message = r.Message,
                    Status = r.Status,
                    CreatedAt = r.CreatedAt,
                    ConfidenceScore = r.ConfidenceScore
                })
                .ToList()
        }, ct);
    }
}
