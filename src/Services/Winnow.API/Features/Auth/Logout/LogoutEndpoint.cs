using FastEndpoints;

namespace Winnow.API.Features.Auth.Logout;

public sealed class LogoutEndpoint : EndpointWithoutRequest
{
    public override void Configure()
    {
        Post("/auth/logout");
        Summary(s =>
        {
            s.Summary = "Log out from the application";
            s.Description = "Removes the authentication cookie from the browser.";
            s.Response(200, "Logout successful");
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        HttpContext.Response.Cookies.Delete("winnow_auth");
        await Send.OkAsync(cancellation: ct);
    }
}
