using MediatR;
using Wolverine;
using Winnow.Contracts;
using Winnow.API.Infrastructure.Security.Authorization;
using Winnow.API.Features.Shared;

using Microsoft.EntityFrameworkCore;
using Winnow.API.Infrastructure.Persistence;

namespace Winnow.API.Features.Clusters.GenerateSummary;

[RequirePermission("clusters:write")]
public record GenerateClusterSummaryCommand(Guid CurrentOrganizationId, Guid Id, Guid ProjectId) : IRequest<GenerateClusterSummaryResult>, IOrgScopedRequest;

public record GenerateClusterSummaryResult(bool IsSuccess, string? ErrorMessage = null, int? StatusCode = null);

public class GenerateClusterSummaryHandler(IMessageBus bus, WinnowDbContext db) : IRequestHandler<GenerateClusterSummaryCommand, GenerateClusterSummaryResult>
{
    public async Task<GenerateClusterSummaryResult> Handle(GenerateClusterSummaryCommand request, CancellationToken cancellationToken)
    {
        var cluster = await db.Clusters.FirstOrDefaultAsync(c => c.Id == request.Id && c.ProjectId == request.ProjectId, cancellationToken);
        if (cluster == null)
        {
            return new GenerateClusterSummaryResult(false, "Cluster not found", 404);
        }

        cluster.StartSummarizing();

        await bus.PublishAsync(new GenerateClusterSummaryEvent(
            request.Id,
            request.CurrentOrganizationId,
            request.ProjectId
        ));

        await db.SaveChangesAsync(cancellationToken);

        return new GenerateClusterSummaryResult(true);
    }
}
