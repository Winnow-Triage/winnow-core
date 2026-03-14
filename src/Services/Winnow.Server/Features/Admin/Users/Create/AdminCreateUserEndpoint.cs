using FastEndpoints;
using MediatR;

namespace Winnow.Server.Features.Admin.Users.Create;

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

public sealed class AdminCreateUserEndpoint(IMediator mediator) : Endpoint<AdminCreateUserRequest, AdminCreateUserResponse>
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
        var command = new AdminCreateUserCommand
        {
            Email = req.Email,
            FullName = req.FullName,
            Password = req.Password,
            Role = req.Role
        };

        try
        {
            var result = await mediator.Send(command, ct);
            await Send.OkAsync(result, ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message);
        }
    }
}

