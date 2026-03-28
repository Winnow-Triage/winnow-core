namespace Winnow.API.Domain.Ai;

public class AiUsage
{
    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public string Context { get; private set; } = string.Empty;
    public string Provider { get; private set; } = string.Empty;
    public string ModelId { get; private set; } = string.Empty;
    public int PromptTokens { get; private set; }
    public int CompletionTokens { get; private set; }
    public int TotalTokens => PromptTokens + CompletionTokens;
    public DateTime CreatedAt { get; private set; }

    private AiUsage() { }

    public AiUsage(Guid organizationId, string context, string provider, string modelId, int promptTokens, int completionTokens)
    {
        Id = Guid.NewGuid();
        OrganizationId = organizationId;
        Context = context;
        Provider = provider;
        ModelId = modelId;
        PromptTokens = promptTokens;
        CompletionTokens = completionTokens;
        CreatedAt = DateTime.UtcNow;
    }
}
