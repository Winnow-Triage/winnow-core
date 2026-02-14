using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Features.Shared;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Reports.SuggestActions;

public class AcceptSuggestionRequest
{
    public Guid Id { get; set; }
}

public class AcceptSuggestionEndpoint(WinnowDbContext db) : Endpoint<AcceptSuggestionRequest, ActionResponse>
{
    public override void Configure()
    {
        Post("/reports/{Id}/accept-suggestion");
        AllowAnonymous();
    }

    public override async Task HandleAsync(AcceptSuggestionRequest req, CancellationToken ct)
    {
        var report = await db.Reports.FindAsync([req.Id], ct);
        if (report == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        if (report.SuggestedParentId == null)
        {
            AddError("Report has no suggested parent to accept.");
            await Send.ErrorsAsync(400, ct);
            return;
        }

        var ultimateParentId = await db.ResolveUltimateMasterAsync(report.SuggestedParentId.Value, ct);

        if (ultimateParentId == report.Id)
        {
            AddError("Cannot accept suggestion: Circular reference detected.");
            await Send.ErrorsAsync(400, ct);
            return;
        }

        report.ParentReportId = ultimateParentId;
        report.Status = "Duplicate";
        report.ConfidenceScore = report.SuggestedConfidenceScore;

        report.SuggestedParentId = null;
        report.SuggestedConfidenceScore = null;

        await db.SaveChangesAsync(ct);

        await Send.OkAsync(new ActionResponse { Message = "Suggestion accepted successfully." }, ct);
    }
}
