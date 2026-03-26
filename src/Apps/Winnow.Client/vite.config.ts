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
    proxy: {
      "/auth": "http://localhost:5294",
      "/admin": "http://localhost:5294",
      "/billing": "http://localhost:5294",
      "/health": "http://localhost:5294",
      "/reports": "http://localhost:5294",
      "/clusters": "http://localhost:5294",
      "/dashboard": "http://localhost:5294",
      "/teams": "http://localhost:5294",
      "/projects": "http://localhost:5294",
      "/account": "http://localhost:5294",
      "/organizations": "http://localhost:5294",
      "/storage": "http://localhost:5294",
    },
  },
  test: {
    globals: true,
    environment: "happy-dom",
    setupFiles: ["./src/test/setup.ts"],
  },
});