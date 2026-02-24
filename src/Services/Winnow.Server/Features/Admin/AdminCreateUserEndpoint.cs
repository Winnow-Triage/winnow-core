using FastEndpoints;
using Microsoft.AspNetCore.Identity;
using Winnow.Server.Entities;

namespace Winnow.Server.Features.Admin;

public class AdminCreateUserRequest
{
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = "Member";
}

public class AdminCreateUserResponse
{
    public string Id { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public sealed class AdminCreateUserEndpoint(
    UserManager<ApplicationUser> userManager,
    ILogger<AdminCreateUserEndpoint> logger) : Endpoint<AdminCreateUserRequest, AdminCreateUserResponse>
{
    public override void Configure()
    {
        Post("/admin/users");
        Roles("SuperAdmin");
        Summary(s =>
        {
            s.Summary = "Create a new user account (SuperAdmin only)";
            s.Description = "Manually creates a new user account with a specified role and password.";
            s.Response<AdminCreateUserResponse>(200, "User created successfully");
            s.Response(400, "Validation failure or email already exists");
        });
    }

    public override async Task HandleAsync(AdminCreateUserRequest req, CancellationToken ct)
    {
        var existing = await userManager.FindByEmailAsync(req.Email);
        if (existing != null)
        {
            ThrowError("A user with this email already exists.");
        }

        var user = new ApplicationUser
        {
            UserName = req.Email,
            Email = req.Email,
            FullName = req.FullName,
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(user, req.Password);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                AddError(error.Description);
            }
            ThrowIfAnyErrors();
        }

        if (!string.IsNullOrEmpty(req.Role))
        {
            await userManager.AddToRoleAsync(user, req.Role);
        }

        logger.LogInformation("SuperAdmin created new user: {Email} with role {Role}", req.Email, req.Role);

        await Send.OkAsync(new AdminCreateUserResponse
        {
            Id = user.Id,
            Message = "User account created successfully."
        }, ct);
    }
}
