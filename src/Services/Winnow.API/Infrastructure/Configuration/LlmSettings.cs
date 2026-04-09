namespace Winnow.API.Infrastructure.Configuration;

public class LlmSettings
{
    public string Provider { get; set; } = "Placeholder"; // "Placeholder", "Ollama", "OpenAI"
    public string EmbeddingProvider { get; set; } = "Onnx"; // "Onnx", "Ollama", "OpenAI", "Placeholder"
    public string ToxicityProvider { get; set; } = "Local"; // "Local", "AmazonComprehend"
    public string PiiRedactionProvider { get; set; } = "Local"; // "Local", "AmazonComprehend"

    public OllamaSettings Ollama { get; set; } = new();
    public OpenAiSettings OpenAI { get; set; } = new();
    public AmazonBedrockSettings Bedrock { get; set; } = new();
    public PresidioSettings Presidio { get; set; } = new();
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

public class AmazonBedrockSettings
{
    public string ModelId { get; set; } = "us.meta.llama3-1-8b-instruct-v1:0";
    public string GatekeeperModelId { get; set; } = "us.meta.llama3-1-8b-instruct-v1:0";
}

public class PresidioSettings
{
    public string AnalyzerEndpoint { get; set; } = "http://localhost:5001";
    public string AnonymizerEndpoint { get; set; } = "http://localhost:5002";
}
