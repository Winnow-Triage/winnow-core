using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FastEndpoints;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Winnow.Server.Entities;
using Winnow.Server.Features.Auth;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Auth;

public class SwitchOrganizationRequest
{
    public Guid OrganizationId { get; set; }
}

public sealed class SwitchOrganizationEndpoint(
    UserManager<ApplicationUser> userManager,
    WinnowDbContext dbContext,
    Winnow.Server.Infrastructure.MultiTenancy.ITenantContext tenantContext,
    IConfiguration config) : Endpoint<SwitchOrganizationRequest, AuthResponse>
{
    public override void Configure()
    {
        Post("/auth/switch");
        Summary(s =>
        {
            s.Summary = "Switch active organization context";
            s.Description = "Generates a new JWT for a different organization the user belongs to without requiring credentials.";
        });
    }

    public override async Task HandleAsync(SwitchOrganizationRequest req, CancellationToken ct)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            ThrowError("Not authenticated");
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user == null)
        {
            ThrowError("User not found");
        }

        // Verify membership
        var membership = await dbContext.OrganizationMembers
            .FirstOrDefaultAsync(om => om.UserId == userId && om.OrganizationId == req.OrganizationId, ct);

        if (membership == null)
        {
            ThrowError("You do not have access to this organization.");
        }

        tenantContext.CurrentOrganizationId = req.OrganizationId;

        // Get any project in this org
        var project = await dbContext.Projects
            .Where(p => p.OrganizationId == req.OrganizationId)
            .OrderBy(p => p.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (project == null)
        {
            ThrowError("This organization has no projects. Please contact an administrator.");
        }

        var token = await GenerateJwtAsync(user, req.OrganizationId);

        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = HttpContext.Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(7)
        };
        HttpContext.Response.Cookies.Append("winnow_auth", token, cookieOptions);

        await Send.OkAsync(new AuthResponse
        {
            UserId = user.Id,
            Email = user.Email ?? "",
            FullName = user.FullName,
            DefaultProjectId = project.Id,
            ApiKey = "",
            ActiveOrganizationId = req.OrganizationId
        });
    }

    private async Task<string> GenerateJwtAsync(ApplicationUser user, Guid organizationId)
    {
        var jwtSettings = config.GetSection("JwtSettings");
        var key = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"] ?? "super_secret_key_at_least_32_chars_long_for_safety");

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email!),
            new(ClaimTypes.Name, user.FullName),
            new("tenant_id", "default"), // Fixed for now or fetch from somewhere
            new("organization", organizationId.ToString())
        };

        var roles = await userManager.GetRolesAsync(user);
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddDays(7),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
            Issuer = jwtSettings["Issuer"],
            Audience = jwtSettings["Audience"]
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
