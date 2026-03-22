# Winnow Service Layer

The service layer contains the core backend components that power the Winnow platform.

## 📂 Services Overview

- [**Winnow.API**](./Winnow.API/README.md): Primary entry point for ingestion, dashboard data, and identity.
- [**Winnow.Sanitize**](./Winnow.Sanitize/README.md): Worker service for PII redaction and toxicity checks.
- [**Winnow.Clustering**](./Winnow.Clustering/README.md): Worker service for semantic grouping and vector search.
- [**Winnow.Summary**](./Winnow.Summary/README.md): Worker service for AI-driven cluster summarization.
- [**Winnow.Bouncer**](./Winnow.Bouncer/README.md): Go-based service for media scanning and virus checks.

## 🏗 Common Patterns

- **Message-Driven**: Services communicate asynchronously via MassTransit and **RabbitMQ**.
- **Containerized**: Every service includes a multi-stage `Dockerfile` (optimized with Linux native dependencies like `libgomp1`).
- **Distributed AI**: Specialized workers handle heavy ONNX and LLM processing to keep the API responsive.

---
For architectural details, see the [Main Documentation Hub](../../docs/README.md).
