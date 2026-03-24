using System.Text.Json.Serialization;

namespace Winnow.Sdk.DotNet.Core.Models;

public class PresignedUrlResponse
{
    [JsonPropertyName("uploadUrl")]
    public string UploadUrl { get; set; }

    [JsonPropertyName("objectKey")]
    public string ObjectKey { get; set; }
}
