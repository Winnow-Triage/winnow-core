# Winnow Server Technical Deep-Dive

The gateway to the Winnow platform, optimized for high-throughput report ingestion, dashboard management, and identity.

## 🚀 API Architecture: FastEndpoints & REPR

Winnow Server uses the **Request-Endpoint-Response (REPR)** pattern via [FastEndpoints](https://fastendpoints.com/). This ensures complete isolation of features and high performance.

### Feature Layout
Endpoints are located in `Features/[MajorArea]/[Action]`.
- **Reports**: Ingestion and search.
- **Clusters**: Dashboard visualization and manual summary triggers.
- **Organizations/Teams**: Multi-tenant management.
- **Billing**: Stripe integration and quota enforcement.

## 🧠 Report Lifecycle & Async Pipeline

The API is the entry point for reports, but analysis is offloaded to specialized workers to maintain sub-50ms ingestion latency.

### 1. Ingestion
Reports arrive via `POST /reports`. The API saves the raw report to Postgres and immediately publishes a `ReportCreatedEvent` to RabbitMQ.

### 2. Distributed Analysis
Specialized workers consume events to process the report:
- **Winnow.Sanitize**: PII redaction and toxicity detection.
- **Winnow.Clustering**: Semantic vector search and grouping (using `pgvector`).
- **Winnow.Summary**: AI-driven cluster summarization.

## 🛡 Native Multi-Tenancy

We use a "Shared Database, Isolated Data" model.
- **Tenant Context**: Extracted from JWT claims or API keys via `TenantMiddleware`.
- **Data Isolation**: `WinnowDbContext` applies **Global Query Filters** to all tenant-aware entities, preventing cross-tenant data leaks.

## 💾 Persistence Layer

### Database Setup
- **PostgreSQL**: Production database for scalability and JSONB support.
- **pgvector**: High-performance vector operations for semantic search directly within PostgreSQL.

### Key Configurations
- **Encryption**: Sensitive credentials are encrypted at rest using AesGcm.
- **Concurrency**: Concurrency tokens and MassTransit filters prevent race conditions during high-volume ingestion.

## 🛠 Running the Server

### Development Mode
```bash
dotnet run --launch-profile "Development"
```
The server applies migrations and seeds initial data (Admin user, Default organization) on startup.

### API Documentation
Visit `/swagger` or `/scalar` for interactive API exploration.
