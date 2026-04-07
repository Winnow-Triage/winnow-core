using MediatR;
using Winnow.API.Infrastructure.Security.Authorization;
using Microsoft.EntityFrameworkCore;
using Winnow.API.Domain.Integrations;
using Winnow.API.Features.Shared;
using Winnow.API.Infrastructure.Persistence;

namespace Winnow.API.Features.Projects.Get;



public class GetIntegrationHandler(WinnowDbContext db)
    : IRequestHandler<GetIntegrationQuery, Integration>
{
    public async Task<Integration> Handle(GetIntegrationQuery request, CancellationToken ct)
    {
        var integration = await db.Integrations
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == request.Id && i.ProjectId == request.ProjectId, ct);

        if (integration == null)
        {
            throw new InvalidOperationException("Integration not found.");
        }

        return integration;
    }
}
