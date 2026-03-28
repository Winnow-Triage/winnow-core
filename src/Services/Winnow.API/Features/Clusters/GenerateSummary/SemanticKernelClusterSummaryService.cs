using System.Text;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Winnow.API.Domain.Reports;
using Winnow.API.Domain.Ai;

namespace Winnow.API.Features.Clusters.GenerateSummary;

public class SemanticKernelClusterSummaryService(Kernel kernel, ILogger<SemanticKernelClusterSummaryService> logger) : IClusterSummaryService
{
    public async Task<ClusterSummaryResult> GenerateSummaryAsync(IEnumerable<Report> reports, CancellationToken ct)
    {
        var reportList = reports.ToList();
        if (reportList.Count == 0) return new ClusterSummaryResult("Empty Cluster", "No reports to summarize.", null, null);

        var sb = new StringBuilder();
        var count = reportList.Count;
        sb.AppendLine("Here are the reports in this cluster:");
        foreach (var report in reportList)
        {
            var stackTrace = report.StackTrace;
            if (stackTrace?.Length > 1500)
            {
                stackTrace = stackTrace[..1500] + "... [TRUNCATED]";
            }

            sb.AppendLine($"- R-{report.Id.ToString()[..8]}: {report.Title}");
            sb.AppendLine($"  Description: {report.Message}");
            if (!string.IsNullOrEmpty(stackTrace))
            {
                sb.AppendLine($"  StackTrace: {stackTrace}");
            }
            sb.AppendLine();
        }

        var prompt = $$"""
            You are an expert technical support analyst. Analyze the following cluster of related support reports.
            Your goal is to provide a concise but highly informative summary that helps a support lead understand the situation at a glance.

            Structure your response with:
            1. **Title**: A very short (3-6 words) descriptive title that identifies the specific error or issue.
            2. **Core Issue**: A one-sentence high-level description of what is happening.
            3. **Common Symptoms**: Bullet points describing how this issue manifests for users.
            4. **Recommended Action**: Suggested next steps for triage or resolution.

            Additionally, assess the CRITICALITY on a scale of 1 to 10. 
            IMPORTANT: Judgement should be based on the **NATURE and SEVERITY** of the problem, NOT the raw number of reports in the cluster. These reports are a signal of a potentially wider issue.

            CRUCIAL ANTI-HALLUCINATION INSTRUCTIONS:
            - If the reports contain generic test data (e.g., "test", "asdf") or lack real error context, DO NOT invent or assume a problem like login failures.
            - Instead, explicitly state in the Core Issue that these are test or uninformative reports.
            - Provide "N/A" for symptoms and recommend "Ignore or delete test data" for action.
            - Assign a Criticality of 1.

            CRITICALITY rubric:
            - **10 (Critical)**: Total system outage, major data loss, critical security breach (PII exposure), or widespread/catastrophic financial corruption.
            - **8-9 (High)**: Core functionality failures (e.g., Login, Payments, Checkout, Data Sync), financial impact to individuals (e.g., double-charging), or severe performance degradation. **Assign these even if only one report is present.**
            - **5-7 (Medium)**: Broken features with workarounds, major usability hurdles, or recurring regressions that impact workflow.
            - **1-4 (Low)**: Cosmetic issues, typos, minor functional bugs with easy workarounds, UI alignment or spacing issues, or general information requests.

            Provide your response in the following JSON format ONLY:
            {
                "title": "Short Descriptive Title",
                "summary": "Markdown string here...",
                "criticalityScore": <int>,
                "criticalityReasoning": "Concise explanation for the assigned score based on the rubric above, explaining the SEVERITY of the issue type."
            }

            Reports ({{count}}):
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

                // If content parsed but fields are missing, provide a meaningful fallback
                if (parsed == null || (string.IsNullOrEmpty(parsed.Title) && string.IsNullOrEmpty(parsed.Summary)))
                {
                    logger.LogWarning("LLM returned empty or invalid JSON structure: {Content}", content);
                    return new ClusterSummaryResult("Unknown Issue", content, null, null);
                }

                return new ClusterSummaryResult(
                    parsed.Title ?? "Untitled Cluster",
                    parsed.Summary ?? "Failed to parse summary field from AI response.",
                    parsed.CriticalityScore,
                    parsed.CriticalityReasoning,
                    Usage: GetUsage(result.Metadata));
            }
            catch (System.Text.Json.JsonException jex)
            {
                logger.LogError(jex, "Failed to parse AI summary JSON. Raw content: {Content}", content);
                return new ClusterSummaryResult("Analysis Parsing Failed", content, null, null, Usage: GetUsage(result.Metadata));
            }
        }
        catch (Exception ex)
        {
            return new ClusterSummaryResult("Generation Failed", $"Failed to generate summary. Error: {ex.Message}", null, null, IsError: true);
        }
    }

    private AiUsageInfo? GetUsage(IReadOnlyDictionary<string, object?>? metadata)
    {
        if (metadata == null) return null;

        if (metadata.TryGetValue("Usage", out var usageObj) && usageObj != null)
        {
            try
            {
                var type = usageObj.GetType();
                var promptTokens = type.GetProperty("InputTokens")?.GetValue(usageObj)
                                   ?? type.GetProperty("InputTokenCount")?.GetValue(usageObj)
                                   ?? type.GetProperty("PromptTokens")?.GetValue(usageObj) ?? 0;
                var completionTokens = type.GetProperty("OutputTokens")?.GetValue(usageObj)
                                       ?? type.GetProperty("OutputTokenCount")?.GetValue(usageObj)
                                       ?? type.GetProperty("CompletionTokens")?.GetValue(usageObj) ?? 0;

                var modelId = metadata.TryGetValue("ModelId", out var m) ? m?.ToString() : null;

                return new AiUsageInfo((int)promptTokens, (int)completionTokens, modelId ?? "unknown", "OpenAI");
            }
            catch { return null; }
        }

        return null;
    }

    private sealed record JsonResult(string Title, string Summary, int? CriticalityScore, string? CriticalityReasoning);
}
