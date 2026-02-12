namespace Winnow.Integrations;

public interface ITicketExporter
{
    Task ExportTicketAsync(string title, string description, CancellationToken cancellationToken);
}

public class TrelloExporter : ITicketExporter
{
    public Task ExportTicketAsync(string title, string description, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[TrelloExporter] Exporting ticket: {title}");
        return Task.CompletedTask;
    }
}
