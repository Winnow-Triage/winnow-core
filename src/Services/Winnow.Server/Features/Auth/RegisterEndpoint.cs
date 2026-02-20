using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FastEndpoints;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Winnow.Server.Entities;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Auth;

/// <summary>
/// Registration request data.
/// </summary>
public class RegisterRequest
{
    /// <summary>
    /// User's full name.
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// User's email address (used as username).
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User's password.
    /// </summary>
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Authentication response containing token and user info.
/// </summary>
public class AuthResponse
{
    /// <summary>
    /// JWT Access Token.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// User's unique identifier.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// User's email address.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User's full name.
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// ID of the user's default project.
    /// </summary>
    public Guid DefaultProjectId { get; set; }

    /// <summary>
    /// API Key for the default project.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
}

public sealed class RegisterEndpoint(
    UserManager<ApplicationUser> userManager,
    WinnowDbContext dbContext,
    Winnow.Server.Infrastructure.MultiTenancy.ITenantContext tenantContext,
    IConfiguration config) : Endpoint<RegisterRequest, AuthResponse>
{
    public override void Configure()
    {
        Post("/auth/register");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Register a new user";
            s.Description = "Creates a new user account, a default organization, and a default project, returning authentication details.";
            s.Response<AuthResponse>(200, "Registration successful");
            s.Response(400, "Registration failed (e.g. email already in use)");
        });
    }

    public override async Task HandleAsync(RegisterRequest req, CancellationToken ct)
    {
        // 1. Create User
        var user = new ApplicationUser
        {
            UserName = req.Email,
            Email = req.Email,
            FullName = req.FullName
        };

        var result = await userManager.CreateAsync(user, req.Password);

        if (!result.Succeeded)
        {
            ThrowError(result.Errors.First().Description);
        }

        // 2. Create Default Organization
        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            Name = $"{req.FullName}'s Organization",
            SubscriptionTier = "free",
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
        
        // 3. Create Default "Personal" Project
        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = $"{req.FullName}'s Project",
            OwnerId = user.Id,
            OrganizationId = organization.Id,
            ApiKey = $"wm_live_{Guid.NewGuid().ToString("N")[..20]}" // Simple API Key gen
        };

        dbContext.Projects.Add(project);
        await dbContext.SaveChangesAsync(ct);
        
        // Set tenant context
        tenantContext.CurrentOrganizationId = organization.Id;

        // 4. Generate JWT
        var token = GenerateJwt(user);

        await Send.OkAsync(new AuthResponse
        {
            Token = token,
            UserId = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            DefaultProjectId = project.Id,
            ApiKey = project.ApiKey
        });
    }

    private string GenerateJwt(ApplicationUser user)
    {
        var jwtSettings = config.GetSection("JwtSettings");
        var key = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"] ?? "super_secret_key_at_least_32_chars_long_for_safety");

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email!),
            new(ClaimTypes.Name, user.FullName)
        };

        // Add organization ID claim if available in tenant context
        if (tenantContext.CurrentOrganizationId.HasValue)
        {
            claims.Add(new Claim("organization", tenantContext.CurrentOrganizationId.Value.ToString()));
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
