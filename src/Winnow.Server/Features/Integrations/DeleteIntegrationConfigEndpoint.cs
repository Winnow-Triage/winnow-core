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
        var config = await db.IntegrationConfigs.FindAsync([id], ct);

        if (config == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        db.IntegrationConfigs.Remove(config);
        await db.SaveChangesAsync(ct);

        await Send.OkAsync(ct);
    }
}
