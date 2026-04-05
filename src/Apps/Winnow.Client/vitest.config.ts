import { defineConfig } from "vitest/config";
import react from "@vitejs/plugin-react";
import path from "path";

export default defineConfig({
  plugins: [react()],
  test: {
    environment: "happy-dom",
    globals: true,
    alias: { "@": path.resolve(__dirname, "./src") },
    setupFiles: ["./setupTests.ts"],
    exclude: ["node_modules", "tests/**"],
    reporters: process.env.CI 
      ? ["default", ["junit", { outputFile: "test-results.xml" }], "github-actions"] 
      : "default",
  },
});
