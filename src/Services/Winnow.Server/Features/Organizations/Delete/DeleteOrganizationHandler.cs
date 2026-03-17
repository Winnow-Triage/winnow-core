using MediatR;
using Winnow.Server.Infrastructure.Security.Authorization;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Infrastructure.Persistence;
using Winnow.Server.Features.Shared;

namespace Winnow.Server.Features.Organizations.Delete;

[RequirePermission("settings:manage")]
public record DeleteOrganizationCommand(Guid CurrentOrganizationId) : IRequest<DeleteOrganizationResult>, IOrgScopedRequest;

public record DeleteOrganizationResult(bool IsSuccess, string? ErrorMessage = null, int? StatusCode = null);

public class DeleteOrganizationHandler(WinnowDbContext db) : IRequestHandler<DeleteOrganizationCommand, DeleteOrganizationResult>
{
    public async Task<DeleteOrganizationResult> Handle(DeleteOrganizationCommand request, CancellationToken cancellationToken)
    {
        var organization = await db.Organizations
            .FirstOrDefaultAsync(o => o.Id == request.CurrentOrganizationId, cancellationToken);

        if (organization == null)
        {
            return new DeleteOrganizationResult(false, "Organization not found", 404);
        }

        db.Organizations.Remove(organization);

        await db.SaveChangesAsync(cancellationToken);

        return new DeleteOrganizationResult(true);
    }
}
