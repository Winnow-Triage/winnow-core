using FastEndpoints;
using MediatR;

namespace Winnow.API.Features.Admin.Users.Delete;

public class DeleteUserRequest
{
    public string UserId { get; set; } = string.Empty;
}

public sealed class DeleteUserEndpoint(IMediator mediator) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Delete("/admin/users/{userId}");
        Roles("SuperAdmin");
        Summary(s =>
        {
            s.Summary = "Delete a user account (SuperAdmin only)";
            s.Description = "Permanently deletes a user account and all associated data records.";
            s.Response(204, "User deleted successfully");
            s.Response(404, "User not found");
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = Route<string>("userId");
        if (string.IsNullOrEmpty(userId))
        {
            AddError("User ID is required");
            ThrowIfAnyErrors(400);
        }

        var command = new DeleteUserCommand { UserId = userId! };

        try
        {
            await mediator.Send(command, ct);
            await Send.NoContentAsync(ct);
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message == "User not found.")
            {
                await Send.NotFoundAsync(ct);
            }
            else
            {
                ThrowError(ex.Message);
            }
        }
    }
}
