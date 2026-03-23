namespace Winnow.API.Domain.Organizations.ValueObjects;

/// <summary>
/// Configuration for AI services at the organization level.
/// </summary>
/// <param name="Tokenizer">The selected tokenizer ID (Default or a Custom Provider ID).</param>
/// <param name="SummaryAgent">The selected summary agent ID (Default or a Custom Provider ID).</param>
/// <param name="CustomProviders">List of user-defined AI providers.</param>
public record AIConfiguration(
    string Tokenizer = "Default",
    string SummaryAgent = "Default",
    List<CustomAIProvider>? CustomProviders = null)
{
    public static AIConfiguration Default => new();

    public IReadOnlyCollection<CustomAIProvider> AllCustomProviders =>
        (CustomProviders ?? []).AsReadOnly();
}

/// <summary>
/// Represents a custom AI provider defined by the organization.
/// </summary>
/// <param name="Name">Display name of the provider (e.g. "My Custom Llama Hub").</param>
/// <param name="Type">The type of service: "Tokenizer" or "SummaryAgent".</param>
/// <param name="ProviderId">The unique identifier used by the backend service.</param>
/// <param name="Provider">The LLM provider (e.g. OpenAI, Anthropic, Ollama).</param>
/// <param name="ApiKey">The API key for the provider.</param>
/// <param name="ModelId">The specific model ID to use.</param>
public record CustomAIProvider(
    string Name,
    string Type,
    string ProviderId,
    string Provider = "OpenAI",
    string ApiKey = "",
    string ModelId = "");
