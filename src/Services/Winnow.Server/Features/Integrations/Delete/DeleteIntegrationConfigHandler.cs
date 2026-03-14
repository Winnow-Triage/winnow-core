using MediatR;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Integrations.Delete;

public record DeleteIntegrationConfigCommand(Guid Id) : IRequest<DeleteIntegrationConfigResult>;

public record DeleteIntegrationConfigResult(bool IsSuccess, string? ErrorMessage = null, int? StatusCode = null);

public class DeleteIntegrationConfigHandler(WinnowDbContext db) : IRequestHandler<DeleteIntegrationConfigCommand, DeleteIntegrationConfigResult>
{
    public async Task<DeleteIntegrationConfigResult> Handle(DeleteIntegrationConfigCommand request, CancellationToken cancellationToken)
    {
        var integration = await db.Integrations.FindAsync([request.Id], cancellationToken);

        if (integration == null)
        {
            return new DeleteIntegrationConfigResult(false, "Integration not found", 404);
        }

        db.Integrations.Remove(integration);
        await db.SaveChangesAsync(cancellationToken);

        return new DeleteIntegrationConfigResult(true);
    }
}
