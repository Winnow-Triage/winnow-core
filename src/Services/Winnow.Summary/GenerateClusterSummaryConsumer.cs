using MassTransit;
using Microsoft.Extensions.Logging;
using Winnow.Contracts;
using Winnow.API.Features.Clusters.GenerateSummary;

namespace Winnow.Summary;

public sealed class GenerateClusterSummaryConsumer(
    ClusterSummaryOrchestrator orchestrator,
    ILogger<GenerateClusterSummaryConsumer> logger) : IConsumer<GenerateClusterSummaryEvent>
{
    public async Task Consume(ConsumeContext<GenerateClusterSummaryEvent> context)
    {
        logger.LogInformation("GenerateClusterSummaryConsumer: Received request for Cluster {ClusterId} (Org: {OrgId})",
            context.Message.ClusterId, context.Message.OrganizationId);

        try
        {
            var success = await orchestrator.GenerateAndChargeAsync(
                context.Message.ClusterId,
                context.Message.ProjectId,
                context.CancellationToken);

            if (success)
            {
                logger.LogInformation("GenerateClusterSummaryConsumer: Successfully generated summary for Cluster {ClusterId}",
                    context.Message.ClusterId);
            }
            else
            {
                logger.LogWarning("GenerateClusterSummaryConsumer: Failed to generate summary for Cluster {ClusterId}. Possibly hit limits or internal error.",
                    context.Message.ClusterId);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "GenerateClusterSummaryConsumer: Error processing summary for Cluster {ClusterId}",
                context.Message.ClusterId);
            // MassTransit will handle retry based on configuration
            throw;
        }
    }
}
