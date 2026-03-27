import path from "path";
import { fileURLToPath } from "url";
import { defineConfig } from "vitest/config";
import react from "@vitejs/plugin-react";
import tailwindcss from "@tailwindcss/vite";

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

export default defineConfig({
  plugins: [tailwindcss(), react()],
  resolve: {
    alias: {
      "@": path.resolve(__dirname, "./src"),
    },
  },
  server: {
    proxy: Object.fromEntries(
      ["/auth", "/admin", "/billing", "/health", "/reports", "/clusters",
       "/dashboard", "/teams", "/projects", "/account", "/organizations", "/storage"
      ].map((route) => [
        route,
        {
          target: "http://localhost:5294",
          // Let browser page navigations (Accept: text/html) fall through to
          // Vite's SPA fallback so index.html is served instead of proxying
          // to the API backend.
          bypass(req: import("http").IncomingMessage) {
            if (req.headers.accept?.includes("text/html")) {
              return "/index.html";
            }
          },
        },
      ])
    ),
  },
  test: {
    globals: true,
    environment: "happy-dom",
    setupFiles: ["./src/test/setup.ts"],
  },
});