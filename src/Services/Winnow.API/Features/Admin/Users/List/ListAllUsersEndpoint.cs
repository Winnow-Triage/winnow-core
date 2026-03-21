using FastEndpoints;
using MediatR;

namespace Winnow.API.Features.Admin.Users.List;

public class UserOrganization
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class UserSummaryResponse
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public bool IsLockedOut { get; set; }
    public List<UserOrganization> Organizations { get; set; } = new();
}

public sealed class ListAllUsersEndpoint(IMediator mediator) : EndpointWithoutRequest<List<UserSummaryResponse>>
{
    public override void Configure()
    {
        Get("/admin/users");
        Roles("SuperAdmin");
        Summary(s =>
        {
            s.Summary = "List all users in the system (SuperAdmin only)";
            s.Description = "Returns a list of all registered users, including their roles and account lockout status.";
            s.Response<List<UserSummaryResponse>>(200, "Success");
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await mediator.Send(new ListAllUsersQuery(), ct);
        await Send.OkAsync(result, ct);
    }
}

