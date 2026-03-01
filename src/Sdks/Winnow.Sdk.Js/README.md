# Winnow JavaScript SDK

A lightweight, TypeScript-first client for integrating Winnow into web and Node.js applications.

## 🚀 Getting Started

```bash
npm install @winnow/sdk-js
```

## ⚙️ Configuration

```typescript
import { WinnowClient } from '@winnow/sdk-js';

const client = new WinnowClient({
  apiKey: 'YOUR_API_KEY_HERE',
  environment: 'production',
  version: '1.0.0'
});
```

## 📖 Basic Usage

### Reporting an Exception
```typescript
try {
  // ... your code
} catch (error) {
  await client.ingestReport({
    title: 'Unexpected Auth Failure',
    message: error.message,
    stackTrace: error.stack,
    severity: 'Error'
  });
}
```

## 🛠 Advanced Features

### Direct Screenshot Upload (S3)
The JS SDK supports the "Presigned URL" flow for large assets. It will automatically request a upload URL from Winnow and push the file directly to S3 to minimize server load.

```typescript
const screenshotBlob = await captureScreenshot();
await client.uploadAsset(screenshotBlob);
```

### Automatic Context Capture
When running in the browser, the SDK automatically captures:
- User Agent / Browser version.
- Screen resolution and viewport size.
- Current URL and referrer.

---
For more details, see the [Main SDK Specifications](../README.md).
