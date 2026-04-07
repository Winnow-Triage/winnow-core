using Wolverine;
using Microsoft.Extensions.Logging;
using Winnow.Contracts;
using Winnow.API.Features.Clusters.GenerateSummary;

namespace Winnow.Summary;

public sealed class GenerateClusterSummaryHandler(
    ClusterSummaryOrchestrator orchestrator,
    ILogger<GenerateClusterSummaryHandler> logger)
{
    public async Task Handle(GenerateClusterSummaryEvent message, CancellationToken cancellationToken)
    {
        logger.LogInformation("GenerateClusterSummaryHandler: Received request for Cluster {ClusterId} (Org: {OrgId})",
            message.ClusterId, message.OrganizationId);

        try
        {
            var success = await orchestrator.GenerateAndChargeAsync(
                message.ClusterId,
                message.ProjectId,
                cancellationToken);

            if (success)
            {
                logger.LogInformation("GenerateClusterSummaryHandler: Successfully generated summary for Cluster {ClusterId}",
                    message.ClusterId);
            }
            else
            {
                logger.LogWarning("GenerateClusterSummaryHandler: Failed to generate summary for Cluster {ClusterId}. Possibly hit limits or internal error.",
                    message.ClusterId);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "GenerateClusterSummaryHandler: Error processing summary for Cluster {ClusterId}",
                message.ClusterId);
            // Wolverine will handle retry based on configuration
            throw;
        }
    }
}
