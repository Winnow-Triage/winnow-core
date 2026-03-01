# Winnow Server Technical Deep-Dive

The core engine of the Winnow platform, optimized for high-throughput report ingestion and intelligent analysis.

## 🚀 API Architecture: FastEndpoints & REPR

Winnow Server eschews traditional MVC controllers in favor of the **Request-Endpoint-Response (REPR)** pattern using [FastEndpoints](https://fastendpoints.com/).

### Why REPR?
- **Isolation**: Each API endpoint is a self-contained class.
- **Developer Speed**: Adding an endpoint doesn't require touching a shared controller.
- **Type Safety**: Request/Response DTOs are strongly typed and local to the endpoint's scope.

### Feature Layout
Endpoints are located in `Features/[MajorArea]/[Action]`.
Example: `Features/Reports/Create/IngestReportEndpoint.cs`

## 🛡 Native Multi-Tenancy

We implement a robust, secure multi-tenant model based on `OrganizationId`.

### How it Works:
1. **Tenant Identification**: The `TenantMiddleware` extracts the tenant ID from the authenticated user's claims.
2. **Context Propagation**: The ID is injected into `ITenantContext`, which is available throughout the request scope.
3. **Data Isolation**: 
   - All tenant-aware entities implement `ITenantEntity`.
   - `WinnowDbContext` applies a **Global Query Filter** to all `ITenantEntity` types:
     `modelBuilder.Entity<T>().HasQueryFilter(e => e.OrganizationId == _tenantContext.CurrentOrganizationId)`
   - This ensures that developers cannot accidentally leak data across tenants.

## 🧠 Report Lifecycle & AI Processing

Processing a report is a multi-stage pipeline designed for speed and intelligence.

### 1. Ingestion
Reports arrive via the `Reports/Create` endpoint. They are immediately persisted and a background event is published via MassTransit.

### 2. Embedding & Vector Search
- **Embedding**: We use an ONNX-runtime based model (`all-MiniLM-L6-v2`) to generate a 384-dimensional vector representation.
- **Vector Search**: Using `sqlite-vec`, we perform a cosine-similarity search against existing reports in the same project.

### 3. Clustering Logic & Thresholds
The `ReportCreatedConsumer` applies the following logic based on cosine distance:
- **Distance <= 0.15**: Automatic **Duplicate** assignment. High confidence match.
- **0.15 < Distance <= 0.35**: **Duplicate Check**. LLM (`duplicateChecker.AreDuplicatesAsync`) is used to confirm if they are true duplicates.
- **0.35 < Distance <= 0.55**: **Suggested Parent**. The report is not merged but a parent cluster is suggested to the user.

### 4. AI Insights (Semantic Kernel)
Once clustered, we use [Semantic Kernel](https://github.com/microsoft/semantic-kernel) to:
- Generate a human-readable **Summary** of the issue.
- **Suggest Actions** for resolution based on the stack trace and context.
- Assign a **Criticality Score** based on business impact.

## 💰 Billing & Quota Enforcement

### Stripe Integration
Located in `Features/Billing` and `Features/Webhooks/StripeWebhookEndpoint.cs`.
- **Flow**: SDK -> IngestEndpoint -> QuotaService.
- **Quota Check**: If an organization breaches its grace limit, the system performs a `RetroactiveRansomAsync` to lock existing records until the subscription is upgraded.

## 💾 Persistence Layer

### Database Setup
- **PostgreSQL**: Production database for scalability and JSONB support.
- **SQLite + sqlite-vec**: Local development and high-performance vector operations.

### Key Configurations
- **Value Conversions**: Enums are stored as strings; JSON blobs are serialized/deserialized automatically.
- **Encryption**: Sensitive integration tokens are encrypted at rest using AesGcm via the `EncryptedStringConverter`.

## 🛠 Running the Server

### Development Mode
```bash
dotnet run --launch-profile "Development"
```
The server will automatically apply migrations and seed necessary data for local testing.

### API Documentation
Visit `/swagger` or `/scalar` to interact with the API via the generated OpenApi spec.
