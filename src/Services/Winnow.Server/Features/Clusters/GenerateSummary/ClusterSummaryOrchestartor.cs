using Microsoft.EntityFrameworkCore;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Clusters.GenerateSummary;

public class ClusterSummaryOrchestrator(
    WinnowDbContext db,
    IClusterSummaryService aiService,
    ILogger<ClusterSummaryOrchestrator> logger)
{
    public async Task<bool> GenerateAndChargeAsync(Guid clusterId, Guid projectId, CancellationToken ct)
    {
        // Load the Aggregates
        var cluster = await db.Clusters.FirstOrDefaultAsync(c => c.Id == clusterId && c.ProjectId == projectId, ct);
        if (cluster == null) return false;

        var organization = await db.Organizations.FindAsync([cluster.OrganizationId], ct);
        if (organization == null) return false;

        // Ask the Aggregate if this is allowed
        if (!organization.CanGenerateAiSummary())
        {
            logger.LogWarning("Organization {OrgId} hit AI summary limit.", organization.Id);
            return false;
        }

        // Fetch context for the LLM
        var clusterReports = await db.Reports
            .AsNoTracking()
            .Where(t => t.ProjectId == projectId && t.ClusterId == clusterId)
            .OrderByDescending(t => t.CreatedAt)
            .Take(20)
            .ToListAsync(ct);

        // Execute the External Service
        var result = await aiService.GenerateSummaryAsync(clusterReports, ct);
        if (result.IsError) return false;

        // Mutate the Aggregates
        cluster.SetSummary(result.Title, result.Summary, result.CriticalityScore ?? 5, result.CriticalityReasoning ?? string.Empty);

        organization.RecordAiSummaryUsage();

        // Atomic Commit
        await db.SaveChangesAsync(ct);

        return true;
    }
}