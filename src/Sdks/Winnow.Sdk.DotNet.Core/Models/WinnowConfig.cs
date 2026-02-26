namespace Winnow.Sdk.DotNet.Core.Models;

public class WinnowConfig
{
    /// <summary>
    /// If true, the SDK will not send data to the server.
    /// Useful for local development or testing.
    /// </summary>
    public bool OfflineMode { get; set; } = false;

    /// <summary>
    /// The environment identifier (e.g., "Development", "Production", "Staging").
    /// </summary>
    public string Environment { get; set; } = "Production";

    /// <summary>
    /// The base URL of the Winnow server. Defaults to the production API.
    /// </summary>
    public string BaseUrl { get; set; } = "https://winnow-api.yourdomain.com"; // Adjust if necessary
}
