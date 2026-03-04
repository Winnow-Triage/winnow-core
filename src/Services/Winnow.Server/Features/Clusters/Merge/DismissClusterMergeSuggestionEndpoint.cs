using System.Security.Claims;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Features.Shared;
using Winnow.Server.Infrastructure.Persistence;
using Winnow.Server.Services.Ai;

namespace Winnow.Server.Features.Clusters.Merge;

public class DismissClusterMergeSuggestionRequest
{
    public Guid Id { get; set; }
}

public sealed class DismissClusterMergeSuggestionEndpoint(WinnowDbContext db, INegativeMatchCache negativeMatchCache)
    : Endpoint<DismissClusterMergeSuggestionRequest, ActionResponse>
{
    public override void Configure()
    {
        Post("/clusters/{id}/dismiss-merge-suggestion");
        Summary(s =>
        {
            s.Summary = "Dismiss a suggested cluster merge";
            s.Description = "Dismisses the AI-suggested merge for the specified cluster and records a negative match.";
            s.Response<ActionResponse>(200, "Suggestion dismissed");
            s.Response(400, "No pending merge suggestion");
            s.Response(404, "Cluster not found");
        });
        Options(x => x.RequireAuthorization());
    }

    public override async Task HandleAsync(DismissClusterMergeSuggestionRequest req, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) ThrowError("Unauthorized", 401);

        if (!HttpContext.Request.Headers.TryGetValue("X-Project-ID", out var projectIdHeader))
        {
            ThrowError("Project ID is required in X-Project-ID header", 400);
        }

        if (!Guid.TryParse(projectIdHeader, out var projectId))
        {
            ThrowError("Invalid Project ID format", 400);
        }

        var cluster = await db.Clusters
            .FirstOrDefaultAsync(c => c.Id == req.Id && c.ProjectId == projectId, ct);

        if (cluster == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        if (cluster.SuggestedMergeClusterId == null)
        {
            ThrowError("No pending merge suggestion for this cluster.");
        }

        // Record negative match between the two clusters
        negativeMatchCache.MarkAsMismatch("default", cluster.Id, cluster.SuggestedMergeClusterId.Value);

        // Clear suggestion
        cluster.SuggestedMergeClusterId = null;
        cluster.SuggestedMergeConfidenceScore = null;

        await db.SaveChangesAsync(ct);
        await Send.OkAsync(new ActionResponse { Message = "Cluster merge suggestion dismissed." }, ct);
    }
}
