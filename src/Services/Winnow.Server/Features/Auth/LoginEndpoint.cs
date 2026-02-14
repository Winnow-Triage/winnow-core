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

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginEndpoint(
    UserManager<ApplicationUser> userManager,
    WinnowDbContext dbContext,
    IConfiguration config) : Endpoint<LoginRequest, AuthResponse>
{
    public override void Configure()
    {
        Post("/auth/login");
        AllowAnonymous();
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

        // Get default project (first one owned by user)
        // In reality, user might have multiple, but we'll return the first one for the "Default Project ID" contract
        var project = await dbContext.Projects
            .Where(p => p.OwnerId == user.Id)
            .OrderBy(p => p.CreatedAt)
            .FirstOrDefaultAsync(ct);
            
        // If no project exists (legacy user?), create one on the fly? Or just return null/empty.
        // For now, let's assume one exists or create a fallback.
        if (project == null)
        {
             project = new Project
            {
                Id = Guid.NewGuid(),
                Name = "Personal Project",
                OwnerId = user.Id,
                ApiKey = $"wm_live_{Guid.NewGuid().ToString("N")[..20]}" 
            };
            dbContext.Projects.Add(project);
            await dbContext.SaveChangesAsync(ct);
        }

        var token = GenerateJwt(user);

        await Send.OkAsync(new AuthResponse
        {
            Token = token,
            UserId = user.Id,
            Email = user.Email ?? "",
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
