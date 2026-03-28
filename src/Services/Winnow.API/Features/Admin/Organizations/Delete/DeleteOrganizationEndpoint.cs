using FastEndpoints;
using MediatR;

namespace Winnow.API.Features.Admin.Organizations.Delete;

public class DeleteOrganizationRequest
{
    public Guid Id { get; set; }
}

/// <summary>
/// Admin endpoint to hard delete an organization.
/// Attempts to clean up S3 assets before deleting the database record.
/// </summary>
public sealed class DeleteOrganizationEndpoint(IMediator mediator) : Endpoint<DeleteOrganizationRequest>
{
    public override void Configure()
    {
        Delete("/admin/organizations/{Id}");
        Roles("SuperAdmin");
        Summary(s =>
        {
            s.Summary = "Hard delete an organization (SuperAdmin only)";
            s.Description = "Permanently deletes an organization and attempts to clean up all associated S3 assets.";
            s.Response(204, "Successfully deleted");
            s.Response(404, "Organization not found");
        });
    }

    public override async Task HandleAsync(DeleteOrganizationRequest req, CancellationToken ct)
    {
        var command = new DeleteOrganizationCommand { Id = req.Id };

        try
        {
            await mediator.Send(command, ct);
            await Send.NoContentAsync(ct);
        }
        catch (InvalidOperationException)
        {
            await Send.NotFoundAsync(ct);
        }
    }
}
