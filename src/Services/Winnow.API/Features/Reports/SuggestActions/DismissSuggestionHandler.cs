using MediatR;
using Microsoft.EntityFrameworkCore;
using Winnow.API.Infrastructure.Persistence;
using Winnow.API.Services.Ai;

namespace Winnow.API.Features.Reports.SuggestActions;

using Winnow.API.Infrastructure.Security.Authorization;
using Winnow.API.Features.Shared;

[RequirePermission("reports:write")]
public record DismissSuggestionCommand(Guid Id, Guid ProjectId, Guid CurrentOrganizationId) : IRequest<DismissSuggestionResult>, IOrgScopedRequest;

public record DismissSuggestionResult(bool IsSuccess, string? Message = null, string? ErrorMessage = null, int? StatusCode = null);

public class DismissSuggestionHandler(WinnowDbContext db, INegativeMatchCache negativeMatchCache) : IRequestHandler<DismissSuggestionCommand, DismissSuggestionResult>
{
    public async Task<DismissSuggestionResult> Handle(DismissSuggestionCommand request, CancellationToken cancellationToken)
    {
        var report = await db.Reports
            .FirstOrDefaultAsync(r => r.Id == request.Id && r.ProjectId == request.ProjectId, cancellationToken);

        if (report == null)
        {
            return new DismissSuggestionResult(false, null, "Report not found", 404);
        }

        if (report.SuggestedClusterId == null)
        {
            return new DismissSuggestionResult(false, null, "No pending suggestion for this report.", 400);
        }

        // Record negative match between report and cluster
        await negativeMatchCache.MarkAsMismatchAsync(request.CurrentOrganizationId.ToString(), report.Id, report.SuggestedClusterId.Value);

        // Clear suggestion
        report.ClearSuggestedCluster();

        await db.SaveChangesAsync(cancellationToken);

        return new DismissSuggestionResult(true, "Suggestion dismissed.");
    }
}
