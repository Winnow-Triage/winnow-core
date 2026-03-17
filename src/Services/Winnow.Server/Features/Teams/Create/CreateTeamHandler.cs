using Winnow.Server.Features.Teams.List;
using MediatR;
using Winnow.Server.Domain.Teams;
using Winnow.Server.Infrastructure.Persistence;

using Winnow.Server.Infrastructure.Security.Authorization;
using Winnow.Server.Features.Shared;

namespace Winnow.Server.Features.Teams.Create;

[RequirePermission("teams:write")]
public record CreateTeamCommand(Guid CurrentOrganizationId, string Name) : IRequest<CreateTeamResult>, IOrgScopedRequest;

public record CreateTeamResult(bool IsSuccess, TeamResponse? Data = null, string? ErrorMessage = null, int? StatusCode = null);

public class CreateTeamHandler(WinnowDbContext db) : IRequestHandler<CreateTeamCommand, CreateTeamResult>
{
    public async Task<CreateTeamResult> Handle(CreateTeamCommand request, CancellationToken cancellationToken)
    {
        var team = new Team(request.CurrentOrganizationId, request.Name);

        db.Teams.Add(team);
        await db.SaveChangesAsync(cancellationToken);

        var data = new TeamResponse
        {
            Id = team.Id,
            Name = team.Name,
            CreatedAt = team.CreatedAt,
            ProjectCount = 0,
            Members = []
        };

        return new CreateTeamResult(true, data);
    }
}
