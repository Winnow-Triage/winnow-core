using MediatR;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Organizations.Delete;

public record DeleteOrganizationCommand(Guid OrganizationId) : IRequest<DeleteOrganizationResult>;

public record DeleteOrganizationResult(bool IsSuccess, string? ErrorMessage = null, int? StatusCode = null);

public class DeleteOrganizationHandler(WinnowDbContext db) : IRequestHandler<DeleteOrganizationCommand, DeleteOrganizationResult>
{
    public async Task<DeleteOrganizationResult> Handle(DeleteOrganizationCommand request, CancellationToken cancellationToken)
    {
        var organization = await db.Organizations
            .FirstOrDefaultAsync(o => o.Id == request.OrganizationId, cancellationToken);

        if (organization == null)
        {
            return new DeleteOrganizationResult(false, "Organization not found", 404);
        }

        db.Organizations.Remove(organization);

        await db.SaveChangesAsync(cancellationToken);

        return new DeleteOrganizationResult(true);
    }
}
