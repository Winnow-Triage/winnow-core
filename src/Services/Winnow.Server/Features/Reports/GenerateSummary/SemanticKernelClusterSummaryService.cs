using System.Text;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Winnow.Server.Entities;

namespace Winnow.Server.Features.Reports.GenerateSummary;

public class SemanticKernelClusterSummaryService(Kernel kernel) : IClusterSummaryService
{
    public async Task<ClusterSummaryResult> GenerateSummaryAsync(IEnumerable<Report> reports, CancellationToken ct)
    {
        var reportList = reports.ToList();
        if (reportList.Count == 0) return new ClusterSummaryResult("No reports to summarize.", null, null);

        var sb = new StringBuilder();
        sb.AppendLine("Here are the reports in this cluster:");
        foreach (var report in reportList)
        {
            sb.AppendLine($"- R-{report.Id.ToString()[..8]}: {report.Title}");
            sb.AppendLine($"  Description: {report.Message}");
            sb.AppendLine($"  StackTrace: {report.StackTrace}");
            sb.AppendLine();
        }

        var prompt = $$"""
            You are an expert technical support analyst. Analyze the following cluster of related support reports.
            Your goal is to provide a concise but highly informative summary that helps a support lead understand the situation at a glance.

            Structure your summary with the following Markdown sections:
            1. **Core Issue**: A one-sentence high-level description of what is happening.
            2. **Common Symptoms**: Bullet points describing how this issue manifests for users.
            3. **Recommended Action**: Suggested next steps for triage or resolution.

            Additionally, assess the CRITICALITY on a scale of 1 to 10. 
            IMPORTANT: Judgement should be based on the **NATURE and SEVERITY** of the problem, NOT the raw number of reports in the cluster. These reports are a signal of a potentially wider issue.

            CRITICALITY rubric:
            - **10 (Critical)**: Total system outage, major data loss, critical security breach (PII exposure), or widespread/catastrophic financial corruption.
            - **8-9 (High)**: Core functionality failures (e.g., Login, Payments, Checkout, Data Sync), financial impact to individuals (e.g., double-charging), or severe performance degradation. **Assign these even if only one report is present.**
            - **5-7 (Medium)**: Broken features with workarounds, major usability hurdles, or recurring regressions that impact workflow.
            - **1-4 (Low)**: Cosmetic issues, typos, minor functional bugs with easy workarounds, or general information requests.

            Provide your response in the following JSON format ONLY:
            {
                "summary": "Markdown string here...",
                "criticalityScore": 5,
                "criticalityReasoning": "Concise explanation for the assigned score based on the rubric above, explaining the SEVERITY of the issue type."
            }

            Reports:
            {{sb}}
            """;

        var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

        var history = new ChatHistory();
        history.AddUserMessage(prompt);

        var executionSettings = new OpenAIPromptExecutionSettings { ResponseFormat = "json_object" };

        try
        {
            var result = await chatCompletionService.GetChatMessageContentAsync(
                history,
                executionSettings: executionSettings,
                kernel: kernel,
                cancellationToken: ct);

            var content = result.Content ?? "{}";

            try
            {
                var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var parsed = System.Text.Json.JsonSerializer.Deserialize<JsonResult>(content, options);
                return new ClusterSummaryResult(parsed?.Summary ?? "Failed to parse summary.", parsed?.CriticalityScore, parsed?.CriticalityReasoning);
            }
            catch
            {
                return new ClusterSummaryResult(content, null, null);
            }
        }
        catch (Exception ex)
        {
            return new ClusterSummaryResult($"Failed to generate summary. Error: {ex.Message}", null, null, IsError: true);
        }
    }

    private record JsonResult(string Summary, int? CriticalityScore, string? CriticalityReasoning);
}
