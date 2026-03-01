# Winnow Client Technical Architecture

`Winnow.Client` is a high-performance, developer-centric dashboard designed for real-time observability.

## 🏗 Component Architecture

We follow a modular component structure designed for reusability and clarity.

### 1. Structural Components
Located in `src/components/`, these provide the "Bones" of the application:
- **Layouts**: `SidebarLayout`, `DashboardLayout`, and `AuthLayout` manage the primary application shell.
- **UI Atomicity**: Custom UI components styled with Tailwind CSS ensure consistency.

### 2. Feature-Based Logic
Components are grouped by feature area (e.g., `src/features/reports/`). Each feature includes its own hooks, sub-components, and API service calls.

## 🌓 State Management Strategy

### 1. Global Infrastructure (Context API)
- **AuthContext**: Manages the authentication lifecycle.
  - *Flow*: `login` -> `AuthService` -> JWT Save -> `useAuth` hook updates state -> App re-renders with authenticated view.
- **ProjectContext**: Holds the currently active project state, allowing for seamless context switching between monitored applications.

### 2. Ephemeral Feature State
For high-frequency or local data (e.g., report lists), we use a combination of local `useState` and custom hooks (e.g., `useReports`). This keeps the global state clean and ensures data is only fetched when needed.

## 📡 Backend Integration Flow

### API Service Layer
All communication with `Winnow.Server` is abstracted through a centralized API service in `src/services/api.ts`.
- **JWT Middleware**: Automatically attaches the Bearer token to all requests.
- **Error Interceptor**: Catches 401/403 errors and triggers the `logout` flow if the session is invalid.

### UI Logic: Paywalls & Access Control
Paywalls are handled dynamically via the `RequireSubscriptionTierFilter.cs` on the backend, while the frontend UI reflects this by disabling specific features or showing "Pro only" badges based on the `PlanTier` claim in the `AuthContext`.

## 🎨 Styling Standards

### Tailwind CSS & Branding
We use a strictly defined Tailwind theme for all colors, fonts, and spacing. Custom design tokens are defined in `tailwind.config.js`.

### User Experience
- **Optimistic UI**: Simple actions (like closing a report) update the UI immediately before the server confirms.
- **Framer Motion**: Subtle transitions for sidebar navigation and modal entries to provide a premium feel.

## 🛠 Development Workflow

```bash
# Install dependencies
npm install

# Start Vite development server
npm run dev

# Run unit and integration tests via Vitest
npm test
```
Vite's HMR is configured to provide sub-millisecond updates during development, ensuring a fast feedback loop.
