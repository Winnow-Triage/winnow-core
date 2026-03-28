using FastEndpoints;
using MediatR;
using Winnow.API.Infrastructure.MultiTenancy;

namespace Winnow.API.Features.Organizations.List;

public class OrganizationDirectoryMemberDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string GlobalRole { get; set; } = string.Empty;
    public Guid RoleId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? JoinedAt { get; set; }
    public bool IsLocked { get; set; }
}

public sealed class ListOrganizationMembersEndpoint(IMediator mediator, ITenantContext tenantContext)
    : EndpointWithoutRequest<List<OrganizationDirectoryMemberDto>>
{
    public override void Configure()
    {
        Get("/organizations/current/members");
        Summary(s =>
        {
            s.Summary = "List all members of the current organization";
            s.Description = "Returns a unified list of active members and pending invitations for the active organization.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!tenantContext.CurrentOrganizationId.HasValue)
        {
            ThrowError("No organization context.");
        }

        var orgId = tenantContext.CurrentOrganizationId.Value;

        var query = new ListOrganizationMembersQuery(orgId);
        var result = await mediator.Send(query, ct);

        if (!result.IsSuccess)
        {
            ThrowError(result.ErrorMessage ?? "Internal Server Error", result.StatusCode ?? 500);
            return;
        }

        await Send.OkAsync(result.Data!, ct);
    }
}
