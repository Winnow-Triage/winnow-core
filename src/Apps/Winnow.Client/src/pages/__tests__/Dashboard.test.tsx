import React from "react";
import { render, screen } from "@testing-library/react";
import "@testing-library/jest-dom";
import Dashboard from "../Dashboard";
import { vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { api } from "@/lib/api";
import { useProject } from "@/context/ProjectContext";
import { BrowserRouter } from "react-router-dom";

// Mock dependencies
vi.mock("@/lib/api", () => ({
  api: {
    get: vi.fn(),
  },
}));

vi.mock("@/context/ProjectContext", () => ({
  useProject: vi.fn(),
}));

const createWrapper = () => {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: {
        retry: false,
      },
    },
  });
  return ({ children }: { children: React.ReactNode }) => (
    <BrowserRouter>
      <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
    </BrowserRouter>
  );
};

describe("Dashboard Component", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    (useProject as any).mockReturnValue({
      currentProject: { id: "p1", name: "Project One" },
    });
  });

  it("renders loading state initially", async () => {
    vi.mocked(api.get).mockImplementation(() => new Promise(() => {}));
    render(<Dashboard />, { wrapper: createWrapper() });
    
    expect(api.get).toHaveBeenCalled();
  });

  it("renders error state when API fails", async () => {
    vi.mocked(api.get).mockRejectedValue(new Error("Network Error"));
    
    render(<Dashboard />, { wrapper: createWrapper() });

    expect(await screen.findByText(/Failed to load dashboard metrics. Network Error/i)).toBeInTheDocument();
  });

  it("renders metrics when API succeeds", async () => {
    const mockData = {
      triage: {
        totalReports: 100,
        activeClusters: 5,
        noiseReductionRatio: 0.85,
        pendingReviews: 12,
        estimatedHoursSaved: 48,
      },
      trendingClusters: [
        { clusterId: "c1", title: "Cluster One", status: "Active", reportCount: 10, velocity: 2, isHot: true }
      ],
      volumeHistory: [
        { timestamp: "2023-10-01", newUniqueCount: 5, duplicateCount: 20 }
      ]
    };

    vi.mocked(api.get).mockResolvedValue({ data: mockData });

    render(<Dashboard />, { wrapper: createWrapper() });

    expect(await screen.findByText(/Dashboard/i)).toBeInTheDocument();
    expect(screen.getByText(/85\s*%/)).toBeInTheDocument(); // Noise reduction ratio in Gauge
    expect(screen.getByText(/48/i)).toBeInTheDocument(); // Hours saved card
    
    // Check pending decisions specifically in its card to avoid button name collision
    const pendingCard = screen.getByText(/Pending Decisions/i).closest(".rounded-3xl");
    expect(pendingCard).toHaveTextContent(/12/);
    
    expect(screen.getByText(/Cluster One/i)).toBeInTheDocument();
  });

  it("does not fetch if no project is selected", () => {
    (useProject as any).mockReturnValue({
      currentProject: null,
    });

    render(<Dashboard />, { wrapper: createWrapper() });
    
    expect(api.get).not.toHaveBeenCalled();
  });
});
