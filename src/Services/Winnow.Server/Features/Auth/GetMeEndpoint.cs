using System.Security.Claims;
using FastEndpoints;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Entities;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Auth;

public class UserMeResponse
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public bool IsEmailVerified { get; set; }
    public List<string> Roles { get; set; } = [];
    public Guid? ActiveOrganizationId { get; set; }
    public Guid? DefaultProjectId { get; set; }
}

public sealed class GetMeEndpoint(
    UserManager<ApplicationUser> userManager,
    WinnowDbContext dbContext) : EndpointWithoutRequest<UserMeResponse>
{
    public override void Configure()
    {
        Get("/auth/me");
        Summary(s =>
        {
            s.Summary = "Get current user information";
            s.Description = "Returns details about the currently authenticated user based on the session cookie.";
            s.Response<UserMeResponse>(200, "User details retrieved successfully");
            s.Response(401, "Not authenticated");
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user == null)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        // Get the organization ID from the claims (set by the JWT middleware)
        var orgIdClaim = User.FindFirstValue("organization");
        Guid? activeOrgId = null;
        if (Guid.TryParse(orgIdClaim, out var parsedOrgId))
        {
            activeOrgId = parsedOrgId;
        }

        // Get default project
        Guid? defaultProjectId = null;
        if (activeOrgId.HasValue)
        {
            var project = await dbContext.Projects
                .Where(p => p.OrganizationId == activeOrgId.Value)
                .OrderBy(p => p.CreatedAt)
                .Select(p => p.Id)
                .FirstOrDefaultAsync(ct);

            if (project != Guid.Empty)
            {
                defaultProjectId = project;
            }
        }

        // Get roles
        var roles = await userManager.GetRolesAsync(user);

        await Send.OkAsync(new UserMeResponse
        {
            Id = user.Id,
            Email = user.Email ?? "",
            FullName = user.FullName,
            IsEmailVerified = user.EmailConfirmed,
            Roles = roles.ToList(),
            ActiveOrganizationId = activeOrgId,
            DefaultProjectId = defaultProjectId
        }, ct);
    }
}
