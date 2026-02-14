using FastEndpoints;
using Winnow.Server.Features.Shared;
using Winnow.Server.Infrastructure.Persistence;
using Winnow.Server.Services.Ai;
using Winnow.Server.Infrastructure.MultiTenancy;

namespace Winnow.Server.Features.Reports.SuggestActions;

public class DismissSuggestionRequest
{
    public bool RejectMatch { get; set; }
}

public class DismissSuggestionEndpoint(WinnowDbContext db, INegativeMatchCache negativeCache, ITenantContext tenantContext) : Endpoint<DismissSuggestionRequest, ActionResponse>
{
    public override void Configure()
    {
        Post("/reports/{Id}/dismiss-suggestion");
        AllowAnonymous();
    }

    public override async Task HandleAsync(DismissSuggestionRequest req, CancellationToken ct)
    {
        var reportId = Route<Guid>("Id");
        var report = await db.Reports.FindAsync([reportId], ct);
        
        if (report == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        if (req.RejectMatch && report.SuggestedParentId.HasValue)
        {
            var tenantId = tenantContext.TenantId ?? "default";
            negativeCache.MarkAsMismatch(tenantId, report.Id, report.SuggestedParentId.Value);
        }

        report.SuggestedParentId = null;
        report.SuggestedConfidenceScore = null;

        await db.SaveChangesAsync(ct);

        await Send.OkAsync(new ActionResponse { Message = "Suggestion dismissed." }, ct);
    }
}
