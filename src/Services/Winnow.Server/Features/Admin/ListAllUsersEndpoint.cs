using FastEndpoints;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Entities;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Admin;

public class UserOrganization
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class UserSummaryResponse
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public bool IsLockedOut { get; set; }
    public List<UserOrganization> Organizations { get; set; } = new();
}

public sealed class ListAllUsersEndpoint(
    UserManager<ApplicationUser> userManager) : EndpointWithoutRequest<List<UserSummaryResponse>>
{
    public override void Configure()
    {
        Get("/admin/users");
        Roles("SuperAdmin");
        Summary(s =>
        {
            s.Summary = "List all users in the system (SuperAdmin only)";
            s.Description = "Returns a list of all registered users, including their roles and account lockout status.";
            s.Response<List<UserSummaryResponse>>(200, "Success");
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var users = await userManager.Users
            .Include(u => u.OrganizationMemberships)
            .ThenInclude(om => om.Organization)
            .ToListAsync(ct);

        var responses = new List<UserSummaryResponse>();

        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            var isLocked = await userManager.IsLockedOutAsync(user);

            responses.Add(new UserSummaryResponse
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FullName = user.FullName,
                Roles = roles.ToList(),
                CreatedAt = user.CreatedAt,
                IsLockedOut = isLocked,
                Organizations = user.OrganizationMemberships.Select(om => new UserOrganization
                {
                    Id = om.OrganizationId,
                    Name = om.Organization?.Name ?? "Unknown"
                }).ToList()
            });
        }

        await Send.OkAsync(responses, ct);
    }
}
