using System.Security.Claims;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Features.Shared;
using Winnow.Server.Infrastructure.Persistence;
using Winnow.Server.Services.Ai;
using Winnow.Server.Infrastructure.MultiTenancy;

namespace Winnow.Server.Features.Reports.SuggestActions;

public class DismissSuggestionRequest
{
    public bool RejectMatch { get; set; }
}

public sealed class DismissSuggestionEndpoint(WinnowDbContext db, INegativeMatchCache negativeCache, ITenantContext tenantContext) : Endpoint<DismissSuggestionRequest, ActionResponse>
{
    public override void Configure()
    {
        Post("/reports/{Id}/dismiss-suggestion");
    }

    public override async Task HandleAsync(DismissSuggestionRequest req, CancellationToken ct)
    {
        // Get user ID from JWT
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            ThrowError("Unauthorized", 401);
        }

        // Get project ID from header
        if (!HttpContext.Request.Headers.TryGetValue("X-Project-ID", out var projectIdHeader))
        {
            ThrowError("Project ID is required in X-Project-ID header", 400);
        }

        if (!Guid.TryParse(projectIdHeader, out var projectId))
        {
            ThrowError("Invalid Project ID format", 400);
        }

        // Validate user owns this project
        var userOwnsProject = await db.Projects
            .AsNoTracking()
            .AnyAsync(p => p.Id == projectId && p.OwnerId == userId, ct);
        
        if (!userOwnsProject)
        {
            ThrowError("Project not found or access denied", 404);
        }

        var reportId = Route<Guid>("Id");
        var report = await db.Reports
            .FirstOrDefaultAsync(r => r.Id == reportId && r.ProjectId == projectId, ct);
        
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
