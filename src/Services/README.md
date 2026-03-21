# Winnow Service Layer

The service layer contains the core backend components that power the Winnow platform.

## 📂 Services Overview

- [**Winnow.API**](./Winnow.API/README.md): The primary C# API and AI processing engine.
- [**Winnow.Bouncer**](./Winnow.Bouncer/README.md): A Go-based service for asset scanning and moderation.
- [**Winnow.Integrations**](./Winnow.Integrations/README.md): Connectivity layer for third-party developer tools.

## 🏗 Common Patterns

- **Message-Driven**: Services communicate via MassTransit and AWS SQS.
- **Dockerized**: Every service includes a `Dockerfile` for standardized deployment.
- **Observability**: Services are instrumented for health checks and performance monitoring.

---
For architectural details, see the [Main Documentation Hub](../../docs/README.md).
