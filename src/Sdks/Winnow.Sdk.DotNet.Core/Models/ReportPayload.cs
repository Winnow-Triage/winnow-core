using System;
using System.Collections.Generic;

namespace Winnow.Sdk.DotNet.Core.Models;

public class ReportPayload
{
    public string Message { get; set; }
    public string StackTrace { get; set; }
    
    // Environmental data
    public string OsVersion { get; set; }
    public string EngineVersion { get; set; }
    public string Platform { get; set; }
    public string Resolution { get; set; }
    
    // User/App specific
    public string AppVersion { get; set; }
    public string UserId { get; set; }

    // S3 Screenshot Key - populated by Step 1 & 2
    public string ScreenshotKey { get; set; }

    // Any extra payload metadata (e.g. game state, scene name)
    public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
}
