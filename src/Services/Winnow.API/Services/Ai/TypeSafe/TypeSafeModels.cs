using System.Text.Json.Serialization;

namespace Winnow.API.Services.Ai.TypeSafe;

public class JevDecisionRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = "typesafe/jev-1.13";

    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;

    [JsonPropertyName("questions")]
    public Dictionary<string, object> Questions { get; set; } = new();
}

public class JevDecisionResponse
{
    [JsonPropertyName("model")]
    public string? Model { get; set; }

    [JsonPropertyName("answers")]
    public Dictionary<string, JevAnswer>? Answers { get; set; }

    [JsonPropertyName("usage")]
    public JevUsage? Usage { get; set; }
}

public class JevAnswer
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("noul")]
    public double? Noul { get; set; }

    [JsonPropertyName("choice")]
    public string? Choice { get; set; }

    [JsonPropertyName("score")]
    public double? Score { get; set; }
}

public class JevUsage
{
    [JsonPropertyName("input_tokens")]
    public int InputTokens { get; set; }

    [JsonPropertyName("output_tokens")]
    public int OutputTokens { get; set; }

    [JsonPropertyName("cost")]
    public double Cost { get; set; }
}
