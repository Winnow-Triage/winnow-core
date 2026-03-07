using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Winnow.Server.Entities;
using Winnow.Server.Infrastructure.Persistence;
using Winnow.Server.Services.Emails;

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

public class RegisterValidator : Validator<RegisterRequest>
{
    public RegisterValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().WithMessage("Full Name is required.");
        RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("A valid email address is required.");
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
            .MaximumLength(128).WithMessage("Password cannot exceed 128 characters.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");
    }
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

    /// <summary>
    /// Whether the user needs to select an organization to complete login.
    /// </summary>
    public bool RequiresOrganizationSelection { get; set; }

    /// <summary>
    /// List of organizations the user belongs to.
    /// </summary>
    public List<OrganizationDto> Organizations { get; set; } = [];

    /// <summary>
    /// The ID of the currently active organization.
    /// </summary>
    public Guid ActiveOrganizationId { get; set; }

    /// <summary>
    /// Whether the user's email is verified.
    /// </summary>
    public bool IsEmailVerified { get; set; }
}

public class OrganizationDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed class RegisterEndpoint(
    UserManager<ApplicationUser> userManager,
    WinnowDbContext dbContext,
    Winnow.Server.Infrastructure.MultiTenancy.ITenantContext tenantContext,
    IConfiguration config,
    Winnow.Server.Infrastructure.Security.IApiKeyService apiKeyService,
    IEmailService emailService) : Endpoint<RegisterRequest, AuthResponse>
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
        Console.WriteLine($"[REGISTER] HandleAsync started for email: {req.Email}");
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
        var organization = new Domain.Organizations.Organization(
            $"{req.FullName}'s Organization",
            new Domain.Common.Email(req.Email)
        );

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
        var projectId = Guid.NewGuid();
        var plaintextKey = apiKeyService.GeneratePlaintextKey(projectId);
        var project = new Domain.Projects.Project(
            organization.Id,
            "Default Project",
            user.Id,
            apiKeyService.HashKey(plaintextKey),
            projectId
        );

        dbContext.Projects.Add(project);
        await dbContext.SaveChangesAsync(ct);

        // Set tenant context
        tenantContext.CurrentOrganizationId = organization.Id;

        // 4. Send Welcome & Verification Emails
        try
        {
            Console.WriteLine($"[REGISTER] Sending welcome email to {user.Email}");
            await emailService.SendWelcomeEmailAsync(user.Email!, user.FullName);
            Console.WriteLine($"[REGISTER] Welcome email sent to {user.Email}");

            // Generate verification token
            var emailToken = await userManager.GenerateEmailConfirmationTokenAsync(user);
            var verificationUrl = $"http://localhost:5173/verify-email?userId={user.Id}&token={Uri.EscapeDataString(emailToken)}";

            Console.WriteLine($"[REGISTER] Sending verification email to {user.Email}");
            await emailService.SendEmailVerificationAsync(user.Email!, new Uri(verificationUrl));
            Console.WriteLine($"[REGISTER] Verification email sent to {user.Email}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[REGISTER] FAILED to send emails to {user.Email}: {ex.Message}");
            // Don't throw, we want registration to succeed even if email fails
            // TODO: Add a way to retry sending the email later
            // TODO: Add an indicator in the system health dashboard that 
        }

        // 5. Generate JWT
        var token = GenerateJwt(user);

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
            Email = user.Email,
            FullName = user.FullName,
            IsEmailVerified = user.EmailConfirmed,
            DefaultProjectId = project.Id,
            ApiKey = plaintextKey,
            ActiveOrganizationId = organization.Id
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
            new(ClaimTypes.Name, user.FullName),
            new("email_verified", user.EmailConfirmed.ToString().ToLowerInvariant())
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
