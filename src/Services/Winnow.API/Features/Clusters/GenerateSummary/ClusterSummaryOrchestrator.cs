using Microsoft.EntityFrameworkCore;
using Winnow.API.Infrastructure.Persistence;

namespace Winnow.API.Features.Clusters.GenerateSummary;

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
            // Even if it failed due to limit, we should clear the summarizing flag
            cluster.FinishSummarizing();
            await db.SaveChangesAsync(ct);
            return false;
        }

        // Fetch context for the LLM
        var clusterReports = await db.Reports
            .AsNoTracking()
            .Where(t => t.ProjectId == projectId && t.ClusterId == clusterId)
            .OrderByDescending(t => t.CreatedAt)
            .Take(20)
            .ToListAsync(ct);

        bool success = false;
        try
        {
            // Execute the External Service
            var result = await aiService.GenerateSummaryAsync(clusterReports, ct);
            if (!result.IsError)
            {
                // Mutate the Aggregates
                cluster.SetSummary(result.Title, result.Summary, result.CriticalityScore ?? 5, result.CriticalityReasoning ?? string.Empty);
                organization.RecordAiSummaryUsage();
                success = true;
            }
        }
        finally
        {
            cluster.FinishSummarizing();
            await db.SaveChangesAsync(ct);
        }

        return success;
    }
}