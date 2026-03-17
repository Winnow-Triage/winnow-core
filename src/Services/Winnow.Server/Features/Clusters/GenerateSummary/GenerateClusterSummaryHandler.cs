using MediatR;

using Winnow.Server.Infrastructure.Security.Authorization;
using Winnow.Server.Features.Shared;

namespace Winnow.Server.Features.Clusters.GenerateSummary;

[RequirePermission("clusters:write")]
public record GenerateClusterSummaryCommand(Guid CurrentOrganizationId, Guid Id, Guid ProjectId) : IRequest<GenerateClusterSummaryResult>, IOrgScopedRequest;

public record GenerateClusterSummaryResult(bool IsSuccess, string? ErrorMessage = null, int? StatusCode = null);

public class GenerateClusterSummaryHandler(ClusterSummaryOrchestrator orchestrator) : IRequestHandler<GenerateClusterSummaryCommand, GenerateClusterSummaryResult>
{
    public async Task<GenerateClusterSummaryResult> Handle(GenerateClusterSummaryCommand request, CancellationToken cancellationToken)
    {
        var success = await orchestrator.GenerateAndChargeAsync(request.Id, request.ProjectId, cancellationToken);

        if (!success)
        {
            return new GenerateClusterSummaryResult(false, "Summary generation failed or quota exceeded.", 400);
        }

        return new GenerateClusterSummaryResult(true);
    }
}
