# Winnow.Clustering Worker

Specialized worker service for semantic report grouping and high-performance vector search.

## 🧠 Responsibilities
- **Embedding Generation**: Uses local ONNX models (e.g., `all-MiniLM-L6-v2`) to generate vector representations of report content.
- **Semantic Matching**: Performs cosine-similarity searches using `pgvector` to identify duplicates or related clusters.
- **Cluster Management**: Dynamically assigns reports to existing clusters or creates new "Untitled Clusters" for novel issues.

## 🏗 Event Flow
1. **Consumes**: `ReportSanitizedEvent` (published by `Winnow.Sanitize`).
2. **Action**: Generates embeddings and assigns clusters.
3. **Publishes**: `GenerateClusterSummaryEvent` once a cluster reaches "Critical Mass" (e.g., 5+ reports).

## 🛠 Tech Stack
- **Vector Engine**: PostgreSQL + `pgvector`.
- **AI**: ONNX Runtime / Semantic Kernel.
- **Messaging**: MassTransit / RabbitMQ.
