using Winnow.API.Features.Teams.List;
using MediatR;
using Winnow.API.Domain.Teams;
using Winnow.API.Infrastructure.Persistence;

using Winnow.API.Infrastructure.Security.Authorization;
using Winnow.API.Features.Shared;

namespace Winnow.API.Features.Teams.Create;

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
