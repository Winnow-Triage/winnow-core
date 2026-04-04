import React from "react";
import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import "@testing-library/jest-dom";
import ReportDetail from "../ReportDetail";
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

vi.mock("react-router-dom", async () => {
  const actual = await vi.importActual("react-router-dom");
  return {
    ...actual,
    useParams: () => ({ id: "r1" }),
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

describe("ReportDetail Component", () => {
  const mockReport = {
    id: "r1",
    title: "Test Report",
    message: "Something went wrong in production.",
    status: "Open",
    createdAt: new Date().toISOString(),
    projectId: "p1",
    assets: [],
    evidence: [],
    isLocked: false,
    isOverage: false,
  };

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("renders report details correctly", async () => {
    vi.mocked(api.get).mockResolvedValue({ data: mockReport });

    render(<ReportDetail />, { wrapper: createWrapper() });

    expect(await screen.findByText("Test Report")).toBeInTheDocument();
    expect(screen.getByText(/Something went wrong in production./i)).toBeInTheDocument();
    expect(screen.getByText(/R-r1/i)).toBeInTheDocument();
  });

  it("renders locked state banner when report is locked", async () => {
    vi.mocked(api.get).mockResolvedValue({
      data: { ...mockReport, isLocked: true }
    });

    render(<ReportDetail />, { wrapper: createWrapper() });

    expect(await screen.findByText(/Report Locked/i)).toBeInTheDocument();
    expect(screen.getByText(/Upgrade to a higher tier to view this stack trace/i)).toBeInTheDocument();
    // Message should be blurred and have lock text (placeholder in blur div)
    expect(screen.getByText(/This content is hidden. Please upgrade your plan to unlock this data./i)).toBeInTheDocument();
  });

  it("handles ungroup action", async () => {
    vi.mocked(api.get).mockResolvedValue({
      data: { ...mockReport, status: "Duplicate", clusterId: "c1", clusterTitle: "Main Cluster" }
    });

    render(<ReportDetail />, { wrapper: createWrapper() });

    expect(await screen.findByText(/This report belongs to a cluster/i)).toBeInTheDocument();
    
    // Click Ungroup
    fireEvent.click(screen.getByRole("button", { name: /Ungroup/i }));
    
    // Check confirmation dialog
    expect(screen.getByText(/Ungroup Report\?/i)).toBeInTheDocument();
    
    // Confirm
    fireEvent.click(screen.getByRole("button", { name: /Confirm/i }));

    await waitFor(() => {
      expect(api.post).toHaveBeenCalledWith("/reports/r1/ungroup", {});
    });
  });
});
