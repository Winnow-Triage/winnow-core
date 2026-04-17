using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Winnow.API.Infrastructure.Identity;
using Winnow.API.Infrastructure.Persistence;

namespace Winnow.API.Features.Admin.Users.Impersonate;

public record ImpersonateUserResult
{
    public string TargetUserEmail { get; init; } = string.Empty;
    public string TokenString { get; init; } = string.Empty;
}

public record ImpersonateUserCommand : IRequest<ImpersonateUserResult>
{
    public string TargetUserId { get; init; } = string.Empty;
    public string AdminEmail { get; init; } = string.Empty;
    public string AdminId { get; init; } = string.Empty;
}

public class ImpersonateUserHandler(
    UserManager<ApplicationUser> userManager,
    IConfiguration config,
    WinnowDbContext dbContext,
    ILogger<ImpersonateUserHandler> logger) : IRequestHandler<ImpersonateUserCommand, ImpersonateUserResult>
{
    public async Task<ImpersonateUserResult> Handle(ImpersonateUserCommand request, CancellationToken cancellationToken)
    {
        var targetUser = await userManager.FindByIdAsync(request.TargetUserId);
        if (targetUser == null)
        {
            throw new InvalidOperationException("User not found.");
        }

        logger.LogWarning("SuperAdmin {AdminEmail} is IMPERSONATING user: {TargetEmail}", request.AdminEmail, targetUser.Email);

        // 1. Resolve their primary organization
        var membership = await dbContext.OrganizationMembers
            .Where(om => om.UserId == targetUser.Id)
            .OrderByDescending(om => om.Role.Name == "Owner")
            .FirstOrDefaultAsync(cancellationToken);

        // 2. Build Claims
        var jwtSettings = config.GetSection("JwtSettings");
        var key = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey configuration is missing"));

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, targetUser.Id),
            new(ClaimTypes.Email, targetUser.Email!),
            new(ClaimTypes.Name, targetUser.FullName),
            new("is_impersonated", "true"),
            new("admin_id", request.AdminId)
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

        return new ImpersonateUserResult
        {
            TargetUserEmail = targetUser.Email ?? "unknown",
            TokenString = tokenString
        };
    }
}

