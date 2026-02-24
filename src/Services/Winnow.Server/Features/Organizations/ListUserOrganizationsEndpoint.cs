using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Features.Auth;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Organizations;

public sealed class ListUserOrganizationsEndpoint(WinnowDbContext db)
    : EndpointWithoutRequest<List<OrganizationDto>>
{
    public override void Configure()
    {
        Get("/organizations");
        Summary(s =>
        {
            s.Summary = "List all organizations the current user belongs to";
            s.Description = "Returns a list of all organizations where the authenticated user is a member.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            ThrowError("User not authenticated.");
        }

        var organizations = await db.OrganizationMembers
            .Where(om => om.UserId == userId)
            .Select(om => new OrganizationDto
            {
                Id = om.OrganizationId,
                Name = om.Organization!.Name
            })
            .ToListAsync(ct);

        await Send.OkAsync(organizations, ct);
    }
}
