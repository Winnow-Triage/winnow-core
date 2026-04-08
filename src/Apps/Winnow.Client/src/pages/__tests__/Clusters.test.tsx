import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen } from "@testing-library/react";
import Clusters from "../Clusters";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { searchClusters } from "@/lib/api";
import { useProject } from "@/hooks/use-project";
import { MemoryRouter } from "react-router-dom";

// Mock hooks
vi.mock("@/hooks/use-project");
vi.mock("@/lib/api", async () => {
  const actual = await vi.importActual("@/lib/api");
  return {
    ...actual,
    searchClusters: vi.fn(),
  };
});

const createWrapper = () => {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: {
        retry: false,
      },
    },
  });
  return ({ children }: { children: React.ReactNode }) => (
    <MemoryRouter>
      <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
    </MemoryRouter>
  );
};

describe("Clusters Component", () => {
  beforeEach(() => {
    vi.mocked(useProject).mockReturnValue({
      currentProject: { id: "p1", name: "Project 1" },
      projects: [],
      isLoading: false,
      setCurrentProject: vi.fn(),
      refreshProjects: vi.fn(),
    });
  });

  it("renders loading state initially", async () => {
    vi.mocked(searchClusters).mockImplementation(() => new Promise(() => {}));
    render(<Clusters />, { wrapper: createWrapper() });
    expect(screen.getByText(/Fetching your clusters.../i)).toBeInTheDocument();
  });

  it("renders no clusters found message when there are no clusters", async () => {
    vi.mocked(searchClusters).mockResolvedValue({
      items: [],
      totalCount: 0,
    });

    render(<Clusters />, { wrapper: createWrapper() });

    expect(await screen.findByText(/No clusters found/i)).toBeInTheDocument();
  });
});
