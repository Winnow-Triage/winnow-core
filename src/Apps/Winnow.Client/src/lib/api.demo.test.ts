import { describe, it, expect } from "vitest";
import { api } from "./api";

describe("API Client - Complete Truman Show Interception", () => {
  
  it("should intercept GET /organizations in demo mode", async () => {
    if (import.meta.env.VITE_DEMO_MODE === "true") {
      await new Promise(r => setTimeout(r, 500));
      const response = await api.get("/organizations");
      expect(response.status).toBe(200);
      expect(Array.isArray(response.data)).toBe(true);
      expect(response.data[0].name).toBe("Winnow Demo Corp");
    }
  });

  it("should intercept GET /projects in demo mode", async () => {
    if (import.meta.env.VITE_DEMO_MODE === "true") {
      const response = await api.get("/projects");
      expect(response.status).toBe(200);
      expect(Array.isArray(response.data)).toBe(true);
      expect(response.data[0].name).toBe("Core API");
    }
  });

  it("should intercept GET /dashboard/metrics in demo mode", async () => {
    if (import.meta.env.VITE_DEMO_MODE === "true") {
      const response = await api.get("/dashboard/metrics");
      expect(response.status).toBe(200);
      expect(response.data.triage).toBeDefined();
      expect(response.data.trendingClusters).toBeDefined();
    }
  });

  it("should return mock user for /auth/me in demo mode", async () => {
    if (import.meta.env.VITE_DEMO_MODE === "true") {
      const response = await api.get("/auth/me");
      expect(response.status).toBe(200);
      expect(response.data.email).toBe("demo@winnow.app");
      expect(response.data.fullName).toBe("Truman Burbank");
    }
  });

  it("should intercept mutations with mock data in demo mode", async () => {
    if (import.meta.env.VITE_DEMO_MODE === "true") {
      const startTime = Date.now();
      const response = await api.put("/account/me", { fullName: "New Name" });
      const duration = Date.now() - startTime;

      expect(response.status).toBe(200);
      expect(response.data.fullName).toBe("New Name");
      expect(duration).toBeGreaterThanOrEqual(300); 
    }
  });
});
