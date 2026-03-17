using MediatR;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Infrastructure.Persistence;

using Winnow.Server.Infrastructure.Security.Authorization;
using Winnow.Server.Features.Shared;

namespace Winnow.Server.Features.Clusters.Get;

[RequirePermission("clusters:read")]
public record GetClusterQuery(Guid CurrentOrganizationId, Guid Id, Guid ProjectId) : IRequest<GetClusterResult>, IOrgScopedRequest;

public record GetClusterResult(bool IsSuccess, GetClusterResponse? Data = null, string? ErrorMessage = null, int? StatusCode = null);

public class GetClusterHandler(WinnowDbContext db) : IRequestHandler<GetClusterQuery, GetClusterResult>
{
    public async Task<GetClusterResult> Handle(GetClusterQuery request, CancellationToken cancellationToken)
    {
        var cluster = await db.Clusters
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.Id && c.ProjectId == request.ProjectId, cancellationToken);

        if (cluster == null)
        {
            return new GetClusterResult(false, null, "Cluster not found", 404);
        }

        var oneHourAgo = DateTime.UtcNow.AddHours(-1);
        var oneDayAgo = DateTime.UtcNow.AddDays(-1);

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
            .FirstOrDefaultAsync(cancellationToken);

        var reports = await db.Reports
            .Where(r => r.ClusterId == cluster.Id)
            .OrderByDescending(r => r.CreatedAt)
            .Take(100)
            .Select(r => new ClusterMemberDto
            {
                Id = r.Id,
                Title = r.Title,
                Message = r.Message,
                Status = r.Status.Name,
                CreatedAt = r.CreatedAt,
                ConfidenceScore = r.ConfidenceScore!.Value.Score
            })
            .ToListAsync(cancellationToken);

        var response = new GetClusterResponse
        {
            Id = cluster.Id,
            ProjectId = cluster.ProjectId,
            Title = cluster.Title,
            Summary = cluster.Summary,
            CriticalityScore = cluster.CriticalityScore,
            CriticalityReasoning = cluster.CriticalityReasoning,
            Status = cluster.Status.ToString(),
            AssignedTo = cluster.AssignedTo,
            CreatedAt = cluster.CreatedAt,
            ReportCount = stats?.Count ?? 0,
            FirstSeen = stats?.FirstSeen,
            LastSeen = stats?.LastSeen,
            Velocity1h = stats?.Velocity1h ?? 0,
            Velocity24h = stats?.Velocity24h ?? 0,
            Reports = reports
        };

        return new GetClusterResult(true, response);
    }
}
