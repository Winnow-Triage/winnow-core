using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FastEndpoints;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Winnow.Server.Infrastructure.Identity;
using Winnow.Server.Domain.Reports.ValueObjects;
using Winnow.Server.Domain.Clusters.ValueObjects;
using Winnow.Server.Infrastructure.Persistence;
using Winnow.Server.Infrastructure.Security;

namespace Winnow.Server.Features.Admin;

public class ImpersonateUserRequest
{
    public string Id { get; set; } = string.Empty;
}

public class ImpersonateUserResponse
{
    public string TargetUserEmail { get; set; } = string.Empty;
}

/// <summary>
/// Allows a SuperAdmin to generate a valid JWT for any user account.
/// THIS IS A HIGHLY SENSITIVE SECURITY FEATURE.
/// </summary>
public sealed class ImpersonateUserEndpoint(
    UserManager<ApplicationUser> userManager,
    IConfiguration config,
    WinnowDbContext dbContext,
    ILogger<ImpersonateUserEndpoint> logger) : Endpoint<ImpersonateUserRequest, ImpersonateUserResponse>
{
    public override void Configure()
    {
        Post("/admin/users/{id}/impersonate");
        Roles("SuperAdmin");
        Summary(s =>
        {
            s.Summary = "Impersonate a user (SuperAdmin only)";
            s.Description = "Generates a session token (JWT) for the target user, allowing an admin to see the app from their perspective.";
            s.Response<ImpersonateUserResponse>(200, "Impersonation token generated");
            s.Response(404, "User not found");
        });
    }

    public override async Task HandleAsync(ImpersonateUserRequest req, CancellationToken ct)
    {
        var targetUser = await userManager.FindByIdAsync(req.Id);
        if (targetUser == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        logger.LogWarning("SuperAdmin {AdminEmail} is IMPERSONATING user: {TargetEmail}",
            User.FindFirstValue(ClaimTypes.Email), targetUser.Email);

        // 1. Resolve their primary organization
        var membership = await dbContext.OrganizationMembers
            .Where(om => om.UserId == targetUser.Id)
            .OrderByDescending(om => om.Role == "owner")
            .FirstOrDefaultAsync(ct);

        // 2. Build Claims
        var jwtSettings = config.GetSection("JwtSettings");
        var key = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"] ?? "super_secret_key_at_least_32_chars_long_for_safety");

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, targetUser.Id),
            new(ClaimTypes.Email, targetUser.Email!),
            new(ClaimTypes.Name, targetUser.FullName),
            new("is_impersonated", "true"),
            new("admin_id", User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown")
        };

        if (membership != null)
        {
            claims.Add(new Claim("organization", membership.OrganizationId.ToString()));
        }

        var roles = await userManager.GetRolesAsync(targetUser);
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        // 3. Generate Short-lived Token (e.g., 2 hours for impersonation)
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(2),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
            Issuer = jwtSettings["Issuer"],
            Audience = jwtSettings["Audience"]
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var securityToken = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(securityToken);

        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = HttpContext.Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddHours(2)
        };
        HttpContext.Response.Cookies.Append("winnow_auth", tokenString, cookieOptions);

        await Send.OkAsync(new ImpersonateUserResponse
        {
            TargetUserEmail = targetUser.Email ?? "unknown"
        }, ct);
    }
}
