# Winnow.Sanitize Worker

Specialized worker service for report sanitization, toxicity detection, and PII redaction.

## 🧠 Responsibilities
- **PII Redaction**: Identifies and masks sensitive information (Names, Emails, Phone Numbers, SSNs) before data reaches the clustering layer.
- **Toxicity Detection**: Scans reports for abusive language or toxic content to ensure safe triaging.
- **Content Cleaning**: Prepares report text for downstream embedding and AI analysis.

## 🏗 Event Flow
1. **Consumes**: `ReportCreatedEvent` (published by `Winnow.API`).
2. **Action**: Performs redaction and toxicity checks. Saves the sanitized report to Postgres.
3. **Publishes**: `ReportSanitizedEvent` to RabbitMQ.

## 🛠 Tech Stack
- **Library**: [Presidio](https://github.com/microsoft/presidio) (PII) / Amazon Comprehend.
- **Messaging**: MassTransit / RabbitMQ.
- **Runtime**: .NET Core 10 (Linux-optimized).
