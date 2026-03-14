using FastEndpoints;
using MediatR;

namespace Winnow.Server.Features.Integrations.Delete;

public sealed class DeleteIntegrationConfigEndpoint(IMediator mediator) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Delete("/integrations/{id}");
        Summary(s =>
        {
            s.Summary = "Delete integration";
            s.Description = "Permanently removes an integration configuration.";
            s.Response(200, "Integration deleted");
            s.Response(404, "Integration not found");
        });
        Options(x => x.RequireAuthorization());
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var id = Route<Guid>("id");

        var command = new DeleteIntegrationConfigCommand(id);
        var result = await mediator.Send(command, ct);

        if (!result.IsSuccess)
        {
            if (result.StatusCode == 404)
            {
                await Send.NotFoundAsync(ct);
                return;
            }
            ThrowError(result.ErrorMessage ?? "Internal Server Error", result.StatusCode ?? 500);
            return;
        }

        await Send.OkAsync(null, ct);
    }
}
