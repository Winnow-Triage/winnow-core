using System.Security.Claims;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Clusters.Assign;

public class AssignClusterRequest
{
    public Guid Id { get; set; }
    public string? AssignedTo { get; set; }
}

public sealed class AssignClusterEndpoint(WinnowDbContext db) : Endpoint<AssignClusterRequest>
{
    public override void Configure()
    {
        Post("/clusters/{id}/assign");
        Options(x => x.RequireAuthorization());
        Summary(s =>
        {
            s.Summary = "Assign a cluster";
            s.Description = "Assigns a cluster to a specific user.";
        });
    }

    public override async Task HandleAsync(AssignClusterRequest req, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) ThrowError("Unauthorized", 401);

        if (!HttpContext.Request.Headers.TryGetValue("X-Project-ID", out var projectIdHeader) ||
            !Guid.TryParse(projectIdHeader, out var projectId))
        {
            ThrowError("Valid Project ID is required in X-Project-ID header", 400);
            return;
        }

        var cluster = await db.Clusters
            .FirstOrDefaultAsync(c => c.Id == req.Id && c.ProjectId == projectId, ct);

        if (cluster == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        cluster.AssignTo(req.AssignedTo);

        await db.SaveChangesAsync(ct);

        await Send.NoContentAsync(ct);
    }
}
