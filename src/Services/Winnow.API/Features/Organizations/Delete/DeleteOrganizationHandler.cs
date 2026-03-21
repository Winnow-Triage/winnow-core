using MediatR;
using Winnow.API.Infrastructure.Security.Authorization;
using Microsoft.EntityFrameworkCore;
using Winnow.API.Infrastructure.Persistence;
using Winnow.API.Features.Shared;

namespace Winnow.API.Features.Organizations.Delete;

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
