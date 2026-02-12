using System.Text;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Winnow.Server.Entities;

namespace Winnow.Server.Features.Tickets.GenerateSummary;

public class SemanticKernelClusterSummaryService(Kernel kernel) : IClusterSummaryService
{
    public async Task<string> GenerateSummaryAsync(IEnumerable<Ticket> tickets, CancellationToken ct)
    {
        var ticketList = tickets.ToList();
        if (ticketList.Count == 0) return "No tickets to summarize.";

        var sb = new StringBuilder();
        sb.AppendLine("Here are the tickets in this cluster:");
        foreach (var ticket in ticketList)
        {
            sb.AppendLine($"- T-{ticket.Id.ToString()[..8]}: {ticket.Title}");
            sb.AppendLine($"  Description: {ticket.Description}");
            sb.AppendLine();
        }

        var prompt = $"""
            You are an expert support agent. Analyze the following group of tickets that have been clustered together as duplicates.
            Identify the core issue, the common symptoms, and any potential solutions mentioned.
            Provide a concise summary of the cluster in markdown format.

            {sb}

            Summary:
            """;

        var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

        // Create a history to maintain context if needed, but for now single turn is fine
        var history = new ChatHistory();
        history.AddUserMessage(prompt);

        var result = await chatCompletionService.GetChatMessageContentAsync(
            history,
            executionSettings: null,
            kernel: kernel,
            cancellationToken: ct);

        return result.Content ?? "Failed to generate summary.";
    }
}
