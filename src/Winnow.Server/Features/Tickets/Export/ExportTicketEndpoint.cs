using FastEndpoints;
using Winnow.Server.Infrastructure.Integrations;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Tickets.Export;

public class ExportTicketRequest
{
    public Guid ConfigId { get; set; }
}

public class ExportTicketEndpoint(WinnowDbContext db, ExporterFactory exporterFactory) : Endpoint<ExportTicketRequest>
{
    public override void Configure()
    {
        Post("/tickets/{Id}/export");
        AllowAnonymous();
    }

    public override async Task HandleAsync(ExportTicketRequest req, CancellationToken ct)
    {
        var ticketId = Route<Guid>("Id");
        var ticket = await db.Tickets.FindAsync([ticketId], ct);

        if (ticket == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var exporter = await exporterFactory.GetExporterByIdAsync(req.ConfigId, ct);
        
        try
        {
            await exporter.ExportTicketAsync(ticket.Title, ticket.Description, ct);
            
            ticket.Status = "Exported";
            await db.SaveChangesAsync(ct);

            await Send.OkAsync(ct);
        }
        catch (Exception ex)
        {
            AddError($"Export failed: {ex.Message}");
            ThrowIfAnyErrors();
        }
    }
}
