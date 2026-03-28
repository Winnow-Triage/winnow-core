# Winnow SDK Engineering Specifications

Winnow SDKs are designed for maximum reliability and minimum impact on host application performance.

## 📡 Ingestion Protocol Design

All Winnow SDKs communicate with the `POST /reports` endpoint using a standardized JSON schema.

### Core Data Structure
The ingestion payload is optimized for both human readability and machine processing:
- **Identifier**: `ProjectId` (Guid) and `OrganizationId` (implicitly via API Key).
- **Core Message**: `Title` and `Message`.
- **System Context**: 
  - `Environment` (e.g., "Production", "Staging").
  - `Version` (The host application's version).
  - `MachineName` / `Platform` details.
- **Diagnostic Data**: 
  - `StackTrace`: Full un-truncated trace.
  - `Metadata`: Arbitrary key-value pairs for custom tagging.
  - `ScreenshotKey`: S3 path for direct uploads (preferred) or legacy `Screenshot` (base64).

### Sample Ingestion Payload
```json
{
  "title": "NullReferenceException in AuthService",
  "message": "Object reference not set to an instance of an object.",
  "stackTrace": "at Winnow.Client.Services.AuthService.LoginAsync...",
  "metadata": {
    "user_id": "12345",
    "feature_flags": ["ai-triaging-v1"]
  },
  "screenshotKey": "uploads/projects/abc/reports/xyz/screenshot.png"
}
```

## 🛡 Resilience & Error Handling

Integrating an observability tool should never crash the host application. We implement several layers of protection:

### 1. Non-Blocking I/O
All SDK calls are asynchronous and non-blocking. The ingestion process is detached from the host app's main thread to ensure zero impact on user-perceived performance.

### 2. Intelligent Retry Policies (Exponential Backoff)
SDKs handle transient network failures using the following defaults:
- **Max Retries**: 3.
- **Backoff Strategy**: Exponential (2^n) with **Jitter** to prevent "Thundering Herd" problems on the server.

### 3. Graceful Shutdown
The SDKs register hooks to ensure any pending reports in the memory buffer are flushed to the server before the host application terminates.

## 🧪 Integration Best Practices

### API Key Security
Never hardcode API Keys in client-side code that is easily decompiled (e.g., Web, Mobile). Use environment variables or secure vault services.

### Criticality Mapping
Use the `Severity` field to differentiate between a minor UI glitch and a system-critical exception. This mapping directly influences the AI's priority ranking and clustering logic.

---
- [**JavaScript/TypeScript SDK Details**](./Winnow.Sdk.Js/README.md)
- [**.NET SDK Details**](./Winnow.Sdk.DotNet/README.md)
