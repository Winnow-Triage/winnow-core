# Contributing to Winnow

Thank you for your interest in contributing to Winnow! This guide outlines the standards and processes for contributing to our monorepo.

## 🏗 Repository Structure

Winnow is a monorepo containing several interconnected services and applications:
- `src/Apps/`: React-based frontend applications (`Winnow.Client`, `Winnow.Marketing`).
- `src/Services/`: Backend services in C# (`Winnow.Server`) and Go (`Winnow.Bouncer`).
- `src/Sdks/`: Client-side libraries for .NET and JS/TS.

## 🛠 Prerequisites

To work on all projects, you will need:
- **.NET 10 SDK** (Backend core)
- **Node.js 18+ & npm** (Frontend & JS SDK)
- **Go 1.22+** (`Winnow.Bouncer`)
- **Docker** (For running PostgreSQL, LocalStack, and other infra)

## 💻 Development Standards

### C# / .NET
- Follow **Vertical Slice Architecture**.
- Use the **REPR Pattern** for all new API endpoints (FastEndpoints).
- Ensure all business entities implement `ITenantEntity` if they are tenant-specific.
- Documentation: Add XML comments to all public service methods and DTOs.

### React / TypeScript
- Use **Atomic Design** principles for UI components.
- Prefer **Functional Components** and Hooks over Class components.
- State: Use Context API for global infrastructure and local state for feature-specific data.
- Styling: All styling must be done via **Tailwind CSS**.

### Go
- Use standard Go project layout (`internal/`, `cmd/`).
- Ensure all new packages include comprehensive unit tests.

## 🧪 Testing

### Backend
Run all .NET tests from the root:
```bash
./run-all-tests.sh
```

### Frontend
Run Vitest for frontend apps:
```bash
cd src/Apps/Winnow.Client && npm test
```

## 🚀 Pull Request Process

1. **Create a Branch**: Use descriptive names like `feat/ai-clustering` or `fix/billing-webhook`.
2. **Implement & Test**: Ensure you've added unit tests for your changes.
3. **Linting**: Run linting tools for your specific project area.
4. **Submit PR**: Provide a clear description of the change and any breaking changes.
5. **Review**: At least one maintainer must approve the PR before merging.

## 📝 Commit Messages

We follow [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/):
- `feat: ...` for new features
- `fix: ...` for bug fixes
- `docs: ...` for documentation changes
- `refactor: ...` for code changes that neither fix a bug nor add a feature

---

Thank you for helping us make Winnow better!
