using FastEndpoints;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Integrations;

public class DeleteIntegrationConfigEndpoint(WinnowDbContext db) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Delete("/integrations/{id}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var id = Route<Guid>("id");
        var integration = await db.Integrations.FindAsync([id], ct);

        if (integration == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        db.Integrations.Remove(integration);
        await db.SaveChangesAsync(ct);

        await Send.OkAsync(null, ct);
    }
}
