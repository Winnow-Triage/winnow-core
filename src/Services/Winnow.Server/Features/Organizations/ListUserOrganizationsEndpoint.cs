using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Organizations;

/// <summary>
/// Organization data transfer object.
/// </summary>
public class OrganizationDto
{
    /// <summary>
    /// Gets or sets the unique identifier of the organization.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the organization.
    /// </summary>
    public string Name { get; set; } = string.Empty;
}


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
            .Join(db.Organizations, om => om.OrganizationId, o => o.Id, (om, o) => new OrganizationDto
            {
                Id = o.Id,
                Name = o.Name
            })
            .ToListAsync(ct);

        await Send.OkAsync(organizations, ct);
    }
}
