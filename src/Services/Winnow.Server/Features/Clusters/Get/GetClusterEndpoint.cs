using System.Security.Claims;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Infrastructure.Persistence;

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

public sealed class GetClusterEndpoint(WinnowDbContext db) : Endpoint<GetClusterRequest, GetClusterResponse>
{
    public override void Configure()
    {
        Get("/clusters/{id:guid}");
        Options(x => x.RequireAuthorization());
    }

    public override async Task HandleAsync(GetClusterRequest req, CancellationToken ct)
    {
        // 1. Fetch ONLY the Cluster aggregate
        var cluster = await db.Clusters
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == req.Id && c.ProjectId == req.ProjectId, ct);

        if (cluster == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var oneHourAgo = DateTime.UtcNow.AddHours(-1);
        var oneDayAgo = DateTime.UtcNow.AddDays(-1);

        // 2. The SQL Math Magic!
        // We do a single, incredibly fast projection query to get all our stats
        var stats = await db.Reports
            .Where(r => r.ClusterId == cluster.Id)
            .GroupBy(r => r.ClusterId)
            .Select(g => new
            {
                Count = g.Count(),
                FirstSeen = (DateTime?)g.Min(r => r.CreatedAt),
                LastSeen = (DateTime?)g.Max(r => r.CreatedAt),
                Velocity1h = g.Count(r => r.CreatedAt >= oneHourAgo),
                Velocity24h = g.Count(r => r.CreatedAt >= oneDayAgo)
            })
            .FirstOrDefaultAsync(ct);

        // 3. Fetch the actual report DTOs directly from the DbSet
        var reports = await db.Reports
            .Where(r => r.ClusterId == cluster.Id)
            .OrderByDescending(r => r.CreatedAt)
            .Take(100) // IMPORTANT: Put a cap on this so we don't return 10MB JSON payloads!
            .Select(r => new ClusterMemberDto
            {
                Id = r.Id,
                Title = r.Title,
                Message = r.Message,
                Status = r.Status.Name,
                CreatedAt = r.CreatedAt,
                ConfidenceScore = r.ConfidenceScore!.Value.Score
            })
            .ToListAsync(ct);

        // 4. Map it all together
        await Send.OkAsync(new GetClusterResponse
        {
            Id = cluster.Id,
            ProjectId = cluster.ProjectId,
            Title = cluster.Title,
            Summary = cluster.Summary,
            CriticalityScore = cluster.CriticalityScore,
            CriticalityReasoning = cluster.CriticalityReasoning,
            Status = cluster.Status.ToString(), // Map from Smart Enum to string
            AssignedTo = cluster.AssignedTo,
            CreatedAt = cluster.CreatedAt,
            ReportCount = stats?.Count ?? 0,
            FirstSeen = stats?.FirstSeen,
            LastSeen = stats?.LastSeen,
            Velocity1h = stats?.Velocity1h ?? 0,
            Velocity24h = stats?.Velocity24h ?? 0,
            Reports = reports
        }, ct);
    }
}