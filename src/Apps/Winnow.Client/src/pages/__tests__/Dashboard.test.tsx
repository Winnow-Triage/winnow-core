import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import Dashboard from "../Dashboard";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { api } from "@/lib/api";
import { useProject } from "@/hooks/use-project";
import { MemoryRouter } from "react-router-dom";

// Mock hooks
vi.mock("@/hooks/use-project");
vi.mock("@/lib/api", async () => {
  const actual = await vi.importActual("@/lib/api");
  return {
    ...actual,
    api: {
      get: vi.fn(),
    },
  };
});

// ResizeObserver mock
global.ResizeObserver = class ResizeObserver {
  observe() {}
  unobserve() {}
  disconnect() {}
};

const createWrapper = () => {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: {
        retry: false,
        staleTime: 0,
        gcTime: 0,
      },
    },
  });
  return ({ children }: { children: React.ReactNode }) => (
    <MemoryRouter>
      <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
    </MemoryRouter>
  );
};

describe("Dashboard Component", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(useProject).mockReturnValue({
      currentProject: { id: "p1", name: "Project 1" },
      projects: [],
      isLoading: false,
      setCurrentProject: vi.fn(),
      refreshProjects: vi.fn(),
    });
  });

  it("renders loading state initially", () => {
    vi.mocked(api.get).mockImplementation(() => new Promise(() => {}));
    render(<Dashboard />, { wrapper: createWrapper() });
    expect(screen.getByText(/Calculating metrics.../i)).toBeInTheDocument();
  });

  it("renders error state when API fails", async () => {
    vi.mocked(api.get).mockRejectedValue({ response: { data: { message: "Network Error" } } });
    render(<Dashboard />, { wrapper: createWrapper() });

    expect(await screen.findByText(/Network Error/i)).toBeInTheDocument();
  });

  it("renders metrics when API succeeds", async () => {
    const mockMetrics = {
      triage: {
        totalReports: 100,
        activeClusters: 10,
        noiseReductionRatio: 0.85,
        pendingReviews: 5,
        estimatedHoursSaved: 20,
      },
      trendingClusters: [],
      volumeHistory: [],
    };
    vi.mocked(api.get).mockResolvedValue({ data: mockMetrics });

    render(<Dashboard />, { wrapper: createWrapper() });

    await waitFor(() => {
      expect(screen.getByText("85%")).toBeInTheDocument();
      expect(screen.getByText(/20/)).toBeInTheDocument();
      expect(screen.getByText(/Hours Saved Today/)).toBeInTheDocument();
      expect(screen.getByText(/Review 5 Suggestions/i)).toBeInTheDocument();
    });
  });

  it("does not fetch if no project is selected", async () => {
    vi.mocked(useProject).mockReturnValue({
      currentProject: null,
      projects: [],
      isLoading: false,
      setCurrentProject: vi.fn(),
      refreshProjects: vi.fn(),
    });

    render(<Dashboard />, { wrapper: createWrapper() });
    
    // Tiny delay to allow any accidental useEffect triggers
    await new Promise(r => setTimeout(r, 10));
    
    expect(api.get).not.toHaveBeenCalled();
  });
});
