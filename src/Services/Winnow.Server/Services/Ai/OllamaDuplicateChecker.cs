using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Winnow.Server.Services.Ai;

public class OllamaDuplicateChecker(
    [FromKeyedServices("Gatekeeper")] IChatCompletionService chatService,
    Winnow.Server.Infrastructure.Configuration.LlmSettings settings,
    ILogger<OllamaDuplicateChecker> logger) : IDuplicateChecker
{
    public async Task<bool> AreDuplicatesAsync(string titleA, string descA, string titleB, string descB, CancellationToken ct)
    {
        // Debug Log to confirm we are using the correct model configuration
        logger.LogInformation("Semantic Gatekeeper: Checking duplicates using model '{ModelId}' (Provider: {Provider})", 
            settings.Ollama.GatekeeperModelId, settings.Provider);

        try
        {
            var prompt = $@"
            You are a Senior QA Engineer. Your job is to determine if two bug reports describe the SAME underlying issue.

            Report A:
            Title: {titleA}
            Description: {descA}

            Report B:
            Title: {titleB}
            Description: {descB}

            Task:
            Analyze the Context, Root Cause (implied), and Symptoms.
            - If one is a ""UI Glitch"" and the other is ""Gameplay Logic"", they are DIFFERENT.
            - If they are effectively the same bug, return TRUE.
            - If they are different or unrelated, return FALSE.

            Respond ONLY with a valid JSON object:
            {{
            ""areDuplicates"": true/false,
            ""reasoning"": ""short explanation""
            }}
            ";

            var history = new ChatHistory();
            history.AddUserMessage(prompt);

            var result = await chatService.GetChatMessageContentAsync(history, cancellationToken: ct);
            var content = result.Content ?? "";

            // Clean up markdown code blocks if present
            content = content.Replace("```json", "").Replace("```", "").Trim();

            try
            {
                var response = JsonSerializer.Deserialize<DuplicateCheckResponse>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (response != null)
                {
                    logger.LogInformation("Semantic Gatekeeper: {Result}. Reasoning: {Reason}", response.AreDuplicates, response.Reasoning);
                    return response.AreDuplicates;
                }
            }
            catch (JsonException)
            {
                // Fallback for non-JSON models
                if (content.Contains("true", StringComparison.OrdinalIgnoreCase)) return true;
                if (content.Contains("false", StringComparison.OrdinalIgnoreCase)) return false;
            }

            return false; // Fail Safe
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Semantic Gatekeeper failed. Defaulting to False (Fail Safe).");
            return false;
        }
    }

    private class DuplicateCheckResponse
    {
        public bool AreDuplicates { get; set; }
        public string? Reasoning { get; set; }
    }
}
