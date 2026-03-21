using MediatR;
using Winnow.API.Infrastructure.Security.Authorization;
using Winnow.API.Infrastructure.Persistence;
using Winnow.API.Features.Shared;

namespace Winnow.API.Features.Integrations.Delete;

[RequirePermission("projects:manage")]
public record DeleteIntegrationConfigCommand(Guid Id, Guid CurrentOrganizationId) : IRequest<DeleteIntegrationConfigResult>, IOrgScopedRequest;

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
