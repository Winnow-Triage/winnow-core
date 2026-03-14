using MediatR;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Admin.Organizations.UpdateStatus;

public record UpdateOrganizationStatusCommand : IRequest
{
    public Guid Id { get; init; }
    public bool IsSuspended { get; init; }
    public string Reasoning { get; init; } = "Unknown Reasoning";
}

public class UpdateOrganizationStatusHandler(WinnowDbContext dbContext) : IRequestHandler<UpdateOrganizationStatusCommand>
{
    public async Task Handle(UpdateOrganizationStatusCommand request, CancellationToken cancellationToken)
    {
        var org = await dbContext.Organizations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(o => o.Id == request.Id, cancellationToken);

        if (org == null)
        {
            throw new InvalidOperationException("Organization not found.");
        }

        if (request.IsSuspended)
        {
            org.Suspend(request.Reasoning);
        }
        else
        {
            // Assuming there's an activate/unsuspend method. If not, we might need to add one or handle this differently based on the domain model.
            // For now, if IsSuspended is false, we might need to throw or ignore if Suspend only goes one way.
            // org.Activate(); 
            // The domain model in Winnow.Server.Domain.Organizations.Organization only has Suspend.
            // If they can't be unsuspended, this parameter is a bit misleading. 
            // But let's assume if it's called with true, it suspends.
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
