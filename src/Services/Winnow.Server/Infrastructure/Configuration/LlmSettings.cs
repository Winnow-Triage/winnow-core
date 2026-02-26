namespace Winnow.Server.Infrastructure.Configuration;

public class LlmSettings
{
    public string Provider { get; set; } = "Placeholder"; // "Placeholder", "Ollama", "OpenAI"
    public string EmbeddingProvider { get; set; } = "Onnx"; // "Onnx", "Ollama", "OpenAI", "Placeholder"
    public OllamaSettings Ollama { get; set; } = new();
    public OpenAiSettings OpenAI { get; set; } = new();
}

public class OllamaSettings
{
    public string Endpoint { get; set; } = "http://localhost:11434";
    public string ModelId { get; set; } = "llama3";
    public string GatekeeperModelId { get; set; } = "phi3";
}

public class OpenAiSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string ModelId { get; set; } = "gpt-4o";
}
