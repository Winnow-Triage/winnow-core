using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FastEndpoints;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Winnow.Server.Entities;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Auth;

/// <summary>
/// Credentials for logging in.
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// User's email address.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User's password.
    /// </summary>
    public string Password { get; set; } = string.Empty;
}

public sealed class LoginEndpoint(
    UserManager<ApplicationUser> userManager,
    WinnowDbContext dbContext,
    Winnow.Server.Infrastructure.MultiTenancy.ITenantContext tenantContext,
    IConfiguration config,
    Winnow.Server.Infrastructure.Security.IApiKeyService apiKeyService) : Endpoint<LoginRequest, AuthResponse>
{
    public override void Configure()
    {
        Post("/auth/login");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Log in to the application";
            s.Description = "Authenticates a user and returns a JWT token along with project details.";
            s.Response<AuthResponse>(200, "Login successful");
            s.Response(400, "Invalid credentials or request");
        });
    }

    public override async Task HandleAsync(LoginRequest req, CancellationToken ct)
    {
        var user = await userManager.FindByEmailAsync(req.Email);
        if (user is null)
        {
            ThrowError("Invalid credentials");
        }

        var validPass = await userManager.CheckPasswordAsync(user, req.Password);
        if (!validPass)
        {
            ThrowError("Invalid credentials");
        }

        // Get user's organization memberships
        var organizationMemberships = await dbContext.OrganizationMembers
            .Where(om => om.UserId == user.Id)
            .ToListAsync(ct);

        if (organizationMemberships.Count == 0)
        {
            // Create a default organization for the user if they don't have one
            var organization = new Organization
            {
                Id = Guid.NewGuid(),
                Name = $"{user.FullName}'s Organization",
                SubscriptionTier = "Free",
                CreatedAt = DateTime.UtcNow
            };

            var organizationMember = new OrganizationMember
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                OrganizationId = organization.Id,
                Role = "owner",
                JoinedAt = DateTime.UtcNow
            };

            dbContext.Organizations.Add(organization);
            dbContext.OrganizationMembers.Add(organizationMember);
            await dbContext.SaveChangesAsync(ct);

            organizationMemberships = new List<OrganizationMember> { organizationMember };
        }

        // Get the first organization (could be the default or user's primary)
        var primaryOrganization = organizationMemberships.First();
        tenantContext.CurrentOrganizationId = primaryOrganization.OrganizationId;

        // Get default project (first one owned by user and in the same organization)
        var project = await dbContext.Projects
            .Where(p => p.OwnerId == user.Id && p.OrganizationId == primaryOrganization.OrganizationId)
            .OrderBy(p => p.CreatedAt)
            .FirstOrDefaultAsync(ct);

        // If no project exists (legacy user?), create one on the fly? Or just return null/empty.
        // For now, let's assume one exists or create a fallback.
        if (project == null)
        {
            var projectId = Guid.NewGuid();
            var plaintextKey = apiKeyService.GeneratePlaintextKey(projectId);
            project = new Project
            {
                Id = projectId,
                Name = "Personal Project",
                OwnerId = user.Id,
                OrganizationId = primaryOrganization.OrganizationId,
                ApiKeyHash = apiKeyService.HashKey(plaintextKey)
            };
            dbContext.Projects.Add(project);
            await dbContext.SaveChangesAsync(ct);
        }

        var token = await GenerateJwtAsync(user);

        await Send.OkAsync(new AuthResponse
        {
            Token = token,
            UserId = user.Id,
            Email = user.Email ?? "",
            FullName = user.FullName,
            DefaultProjectId = project.Id,
            ApiKey = "" // Cannot return hashed key
        });
    }

    private async Task<string> GenerateJwtAsync(ApplicationUser user)
    {
        var jwtSettings = config.GetSection("JwtSettings");
        var key = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"] ?? "super_secret_key_at_least_32_chars_long_for_safety");

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email!),
            new(ClaimTypes.Name, user.FullName),
            new("tenant_id", tenantContext.TenantId ?? "default")
        };

        // Add organization ID claim if user has one
        if (tenantContext.CurrentOrganizationId.HasValue)
        {
            claims.Add(new Claim("organization", tenantContext.CurrentOrganizationId.Value.ToString()));
        }

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
