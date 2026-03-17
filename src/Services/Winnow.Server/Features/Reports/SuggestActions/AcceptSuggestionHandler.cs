using MediatR;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Domain.Reports.ValueObjects;
using Winnow.Server.Infrastructure.Persistence;
using Winnow.Server.Services.Ai;

namespace Winnow.Server.Features.Reports.SuggestActions;

using Winnow.Server.Infrastructure.Security.Authorization;
using Winnow.Server.Features.Shared;

[RequirePermission("reports:write")]
public record AcceptSuggestionCommand(Guid CurrentOrganizationId, Guid Id, Guid ProjectId) : IRequest<AcceptSuggestionResult>, IOrgScopedRequest;

public record AcceptSuggestionResult(bool IsSuccess, string? Message = null, string? ErrorMessage = null, int? StatusCode = null);

public class AcceptSuggestionHandler(WinnowDbContext db, IClusterService clusterService) : IRequestHandler<AcceptSuggestionCommand, AcceptSuggestionResult>
{
    public async Task<AcceptSuggestionResult> Handle(AcceptSuggestionCommand request, CancellationToken cancellationToken)
    {
        var report = await db.Reports
            .FirstOrDefaultAsync(r => r.Id == request.Id && r.ProjectId == request.ProjectId, cancellationToken);

        if (report == null)
        {
            return new AcceptSuggestionResult(false, null, "Report not found", 404);
        }

        if (report.SuggestedClusterId == null)
        {
            return new AcceptSuggestionResult(false, null, "No pending suggestion for this report.", 400);
        }

        // Verify suggested cluster exists
        var cluster = await db.Clusters.FindAsync([report.SuggestedClusterId], cancellationToken);
        if (cluster == null)
        {
            return new AcceptSuggestionResult(false, null, "The suggested cluster no longer exists.", 400);
        }

        // Accept suggestion
        cluster.AddReport(report.Id);
        report.AssignToCluster(cluster.Id, report.SuggestedConfidenceScore);
        report.ChangeStatus(ReportStatus.Duplicate);
        report.ClearSuggestedCluster();

        await clusterService.RecalculateCentroidAsync(cluster.Id, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);

        return new AcceptSuggestionResult(true, "Suggestion accepted. Report added to cluster.");
    }
}
