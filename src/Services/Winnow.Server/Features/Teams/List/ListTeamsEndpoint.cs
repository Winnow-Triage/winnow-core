using FastEndpoints;
using MediatR;
using Winnow.Server.Infrastructure.MultiTenancy;

namespace Winnow.Server.Features.Teams.List;

public class TeamResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int ProjectCount { get; set; }
    public List<TeamMemberSummary> Members { get; set; } = [];
    public List<TeamProjectSummary> Projects { get; set; } = [];
}

public class TeamProjectSummary
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class TeamMemberSummary
{
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
}

public sealed class ListTeamsEndpoint(IMediator mediator, ITenantContext tenantContext)
    : EndpointWithoutRequest<List<TeamResponse>>
{
    public override void Configure()
    {
        Get("/teams");
        Summary(s =>
        {
            s.Summary = "List all teams in the current organization";
            s.Description = "Returns a list of all teams belonging to the active organization.";
        });
        Options(x => x.RequireAuthorization());
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!tenantContext.CurrentOrganizationId.HasValue)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var orgId = tenantContext.CurrentOrganizationId.Value;

        var query = new ListTeamsQuery(orgId);
        var result = await mediator.Send(query, ct);

        if (!result.IsSuccess)
        {
            ThrowError(result.ErrorMessage ?? "Internal Server Error", result.StatusCode ?? 500);
            return;
        }

        await Send.OkAsync(result.Data!, ct);
    }
}