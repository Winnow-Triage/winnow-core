using FastEndpoints;
using MediatR;
using Winnow.Server.Features.Shared;

namespace Winnow.Server.Features.Clusters.GenerateSummary;

public class GenerateClusterSummaryRequest : ProjectScopedRequest
{
    public Guid Id { get; set; }
}

public sealed class GenerateClusterSummaryEndpoint(IMediator mediator) : ProjectScopedEndpoint<GenerateClusterSummaryRequest, ActionResponse>
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
        var command = new GenerateClusterSummaryCommand(req.Id, req.CurrentProjectId);
        var result = await mediator.Send(command, ct);

        if (!result.IsSuccess)
        {
            ThrowError(result.ErrorMessage ?? "Summary generation failed or quota exceeded.", result.StatusCode ?? 400);
            return;
        }

        await Send.OkAsync(new ActionResponse { Message = "Summary generated successfully." }, ct);
    }
}