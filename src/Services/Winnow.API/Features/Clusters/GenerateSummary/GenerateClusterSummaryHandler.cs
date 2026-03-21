using MediatR;
using MassTransit;
using Winnow.Contracts;
using Winnow.API.Infrastructure.Security.Authorization;
using Winnow.API.Features.Shared;

namespace Winnow.API.Features.Clusters.GenerateSummary;

[RequirePermission("clusters:write")]
public record GenerateClusterSummaryCommand(Guid CurrentOrganizationId, Guid Id, Guid ProjectId) : IRequest<GenerateClusterSummaryResult>, IOrgScopedRequest;

public record GenerateClusterSummaryResult(bool IsSuccess, string? ErrorMessage = null, int? StatusCode = null);

public class GenerateClusterSummaryHandler(IPublishEndpoint publishEndpoint) : IRequestHandler<GenerateClusterSummaryCommand, GenerateClusterSummaryResult>
{
    public async Task<GenerateClusterSummaryResult> Handle(GenerateClusterSummaryCommand request, CancellationToken cancellationToken)
    {
        await publishEndpoint.Publish(new GenerateClusterSummaryEvent(
            request.Id,
            request.CurrentOrganizationId,
            request.ProjectId
        ), cancellationToken);

        return new GenerateClusterSummaryResult(true);
    }
}
