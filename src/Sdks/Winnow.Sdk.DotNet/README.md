# Winnow .NET SDK

The official .NET SDK for Winnow, supporting .NET 8, 9, and 10.

## 📦 Packages

- **Winnow.Sdk.Core**: The core ingestion client for any C# application.
- **Winnow.Sdk.Unity**: Optimized for mobile and desktop Unity games.
- **Winnow.Sdk.Godot**: Dedicated support for the Godot engine.

## ⚙️ Configuration

### appsettings.json
```json
{
  "Winnow": {
    "ApiKey": "YOUR_API_KEY_HERE",
    "Environment": "Production"
  }
}
```

### Dependency Injection
```csharp
builder.Services.AddWinnow(options => {
    options.ApiKey = "sk_...";
});
```

## 📖 Basic Usage

```csharp
using Winnow.Sdk;

public class MyService(IWinnowClient winnow)
{
    public async Task DoWork()
    {
        try {
            // ...
        } catch (Exception ex) {
            await winnow.IngestReportAsync(ex, new { CustomData = "Value" });
        }
    }
}
```

## 🎮 Game Engine Support

### Unity
The Unity SDK includes an automatic "Crash Handler" that hooks into `Application.logMessageReceived`.

### Godot
The Godot SDK provides a singleton node (`WinnowNode`) to capture signals from across your scene tree.

---
For more details, see the [Main SDK Specifications](../README.md).
