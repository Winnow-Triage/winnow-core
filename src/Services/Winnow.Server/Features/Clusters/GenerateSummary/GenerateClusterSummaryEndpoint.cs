using FastEndpoints;
using Winnow.Server.Features.Shared;

namespace Winnow.Server.Features.Clusters.GenerateSummary;

public class GenerateClusterSummaryRequest : ProjectScopedRequest
{
    public Guid Id { get; set; }
}

public sealed class GenerateClusterSummaryEndpoint(ClusterSummaryOrchestrator orchestrator) : ProjectScopedEndpoint<GenerateClusterSummaryRequest, ActionResponse>
{
    public override void Configure()
    {
        Post("/clusters/{Id}/generate-summary");
        Summary(s =>
        {
            s.Summary = "Generate cluster summary";
            s.Description = "Triggers AI generation of a summary and criticality score for a cluster.";
            s.Response<ActionResponse>(200, "Summary generated successfully");
            s.Response(404, "Cluster not found");
            s.Response(402, "Payment Required");
            s.Response(500, "AI generation failed");
        });
        Options(x => x.RequireAuthorization());
    }

    public override async Task HandleAsync(GenerateClusterSummaryRequest req, CancellationToken ct)
    {
        var success = await orchestrator.GenerateAndChargeAsync(req.Id, req.CurrentProjectId, ct);

        if (!success)
        {
            ThrowError("Summary generation failed or quota exceeded.", 400);
        }

        await Send.OkAsync(new ActionResponse { Message = "Summary generated successfully." }, ct);
    }
}