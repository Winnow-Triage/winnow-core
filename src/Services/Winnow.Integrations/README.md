# Winnow.Integrations: The Connectivity Layer

`Winnow.Integrations` is a dedicated service for bridging Winnow with third-party developer tools and collaboration platforms.

## 🏗 Responsibilities

This service handles the "Outbound" flow of data from Winnow to external ecosystems:
- **Exporting Reports**: Transforming and pushing bug reports to Jira, GitHub Issues, or Linear.
- **Notifications**: Sending alerts to Slack, Discord, or Microsoft Teams.
- **Customer Feedback**: Syncing with CRM tools like Zendesk or Intercom.

## 🔌 Integration Architecture

### Core Interface: `IReportExporter`
Every integration provider (e.g., `JiraExporter`) must implement the `IReportExporter` interface. This ensures a consistent contract for:
- Authentication with the third-party API.
- Mapping Winnow's internal `Report` entity to the external platform's "Ticket" or "Issue" schema.
- Handling rate limits and connectivity retries.

### Domain Config: `IntegrationConfig`
Located in `Domain/IntegrationConfig.cs`, this class manages the per-organization configuration for each integration, including:
- **Encrypted Tokens**: API keys or OAuth tokens stored securely.
- **Mapping Rules**: Custom logic for which project in Winnow maps to which project in the external tool.

## 🛠 Adding a New Integration

1. Define a new configuration class inheriting from `IntegrationConfigBase`.
2. Implement the `IReportExporter` interface.
3. Register the new exporter in the `ServiceCollection` extensions.
4. Add the necessary webhook handlers for bi-directional sync if required.

---
Integrations allow Winnow to fit seamlessly into any existing engineering workflow.
