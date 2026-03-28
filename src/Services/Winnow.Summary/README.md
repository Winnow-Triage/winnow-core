# Winnow.Summary Worker

Specialized worker service for AI-driven summarization and criticality analysis of report clusters.

## 🧠 Responsibilities
- **AI Summarization**: Uses LLMs (OpenAI/Ollama) to generate human-readable titles and executive summaries for clusters.
- **Criticality Scoring**: Analyzes cluster context to assign an impact score (1-10) and provides reasoning.
- **Action Items**: Suggests potential fixes or debugging steps based on clustered error data.

## 🏗 Event Flow
1. **Consumes**: `GenerateClusterSummaryEvent` (published by `Winnow.Clustering` or manually triggered by `Winnow.API`).
2. **Action**: Invokes the `ClusterSummaryOrchestrator` to generate the AI summary.
3. **Updates**: Directly updates the `Cluster` entity in the Postgres database.

## 🛠 Tech Stack
- **AI Orchestration**: [Semantic Kernel](https://github.com/microsoft/semantic-kernel).
- **LLM Providers**: OpenAI API / Ollama (Local).
- **Messaging**: MassTransit / RabbitMQ.
