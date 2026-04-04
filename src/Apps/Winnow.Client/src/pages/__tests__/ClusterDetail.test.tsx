import React from "react";
import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import "@testing-library/jest-dom";
import ClusterDetail from "../ClusterDetail";
import { vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { api } from "@/lib/api";
import { BrowserRouter } from "react-router-dom";

// Mock dependencies
vi.mock("@/lib/api", () => ({
  api: {
    get: vi.fn(),
    post: vi.fn(),
  },
}));

vi.mock("react-markdown", () => ({
  default: ({ children }: any) => <div>{children}</div>,
}));

vi.mock("react-router-dom", async () => {
  const actual = await vi.importActual("react-router-dom");
  return {
    ...actual,
    useParams: () => ({ id: "c1" }),
    useNavigate: () => vi.fn(),
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
    <BrowserRouter>
      <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
    </BrowserRouter>
  );
};

describe("ClusterDetail Component", () => {
  const mockCluster = {
    id: "c1",
    projectId: "p1",
    title: "Main Bug",
    summary: "AI summary of the problem.",
    criticalityScore: 7,
    criticalityReasoning: "High impact on users.",
    status: "Open",
    createdAt: new Date().toISOString(),
    reportCount: 10,
    velocity1h: 1,
    velocity24h: 5,
    reports: [
      { id: "r1", title: "Report One", status: "Duplicate", createdAt: new Date().toISOString() },
      { id: "r2", title: "Report Two", status: "Duplicate", createdAt: new Date().toISOString() },
    ],
  };

  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(api.get).mockImplementation((url) => {
        if (url === "/billing/status") return Promise.resolve({ data: { subscriptionTier: "Pro" } });
        if (url === "/clusters/c1") return Promise.resolve({ data: mockCluster });
        if (url === "/projects/p1/integrations") return Promise.resolve({ data: [] });
        return Promise.reject(new Error("Not found"));
    });
  });

  it("renders cluster details correctly", async () => {
    render(<ClusterDetail />, { wrapper: createWrapper() });

    expect(await screen.findByText("Main Bug")).toBeInTheDocument();
    expect(screen.getByText(/AI summary of the problem./i)).toBeInTheDocument();
    expect(screen.getByText(/Criticality:/i)).toBeInTheDocument();
    expect(screen.getByText(/7\s*\/\s*10/)).toBeInTheDocument();
    expect(screen.getByText("10")).toBeInTheDocument();
    expect(screen.getByText(/Reports Impacted/i)).toBeInTheDocument();
  });

  it("renders reports list", async () => {
    render(<ClusterDetail />, { wrapper: createWrapper() });

    expect(await screen.findByText("Impacted Reports")).toBeInTheDocument();
    expect(screen.getByText("Report One")).toBeInTheDocument();
    expect(screen.getByText("Report Two")).toBeInTheDocument();
  });

  it("triggers AI analysis", async () => {
    render(<ClusterDetail />, { wrapper: createWrapper() });

    // Click analysis button (it's inside dropdown if summary exists, but let's check for the button)
    // Actually, based on ClusterDetail.tsx, if summary exists it's in a DropdownMenuItem
    // We can just verify the post call if triggered
    
    // Let's test the "Start AI Analysis" button when summary is missing
    vi.mocked(api.get).mockImplementation((url) => {
        if (url === "/billing/status") return Promise.resolve({ data: { subscriptionTier: "Pro" } });
        if (url === "/clusters/c1") return Promise.resolve({ data: { ...mockCluster, summary: null } });
        if (url === "/projects/p1/integrations") return Promise.resolve({ data: [] });
        return Promise.reject(new Error("Not found"));
    });

    render(<ClusterDetail />, { wrapper: createWrapper() });

    const btn = await screen.findByRole("button", { name: /Start AI Analysis/i });
    fireEvent.click(btn);

    await waitFor(() => {
      expect(api.post).toHaveBeenCalledWith("/clusters/c1/generate-summary", {});
    });
  });

  it("shows resolution confirmation", async () => {
      render(<ClusterDetail />, { wrapper: createWrapper() });

      const closeBtn = await screen.findByRole("button", { name: /Close Cluster/i });
      fireEvent.click(closeBtn);

      expect(screen.getByText(/Resolve Cluster\?/i)).toBeInTheDocument();
      expect(screen.getByText(/mark all 10 reports in this cluster as Closed/i)).toBeInTheDocument();
  });
});
