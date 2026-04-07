import React from "react";
import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import "@testing-library/jest-dom";
import ProjectSettings from "../ProjectSettings";
import { vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { api, rotateProjectApiKey } from "@/lib/api";
import { useProject } from "@/context/ProjectContext";
import { BrowserRouter } from "react-router-dom";
import { toast } from "sonner";

// Mock dependencies
vi.mock("@/lib/api", () => ({
  api: {
    get: vi.fn(),
    put: vi.fn(),
    post: vi.fn(),
    delete: vi.fn(),
  },
  rotateProjectApiKey: vi.fn(),
  revokeProjectSecondaryApiKey: vi.fn(),
}));

vi.mock("@/context/ProjectContext", () => ({
  useProject: vi.fn(),
}));

vi.mock("sonner", () => ({
  toast: {
    success: vi.fn(),
    error: vi.fn(),
  },
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

describe("ProjectSettings Component", () => {
  const mockUpdateProjectSettings = vi.fn();
  const mockDeleteProject = vi.fn();
  const mockRefreshProjects = vi.fn();

  beforeEach(() => {
    vi.clearAllMocks();
    (useProject as any).mockReturnValue({
      currentProject: { id: "p1", name: "Project One", hasSecondaryKey: false },
      updateProjectSettings: mockUpdateProjectSettings,
      deleteProject: mockDeleteProject,
      refreshProjects: mockRefreshProjects,
    });
    vi.mocked(api.get).mockResolvedValue({ data: [] }); // Integrations list
  });

  it("renders project general settings", async () => {
    render(<ProjectSettings />, { wrapper: createWrapper() });
    
    expect(screen.getByText(/Project Configuration/i)).toBeInTheDocument();
    expect(screen.getByDisplayValue("Project One")).toBeInTheDocument();
  });

  it("handles project rename successfully", async () => {
    mockUpdateProjectSettings.mockResolvedValueOnce(undefined);

    render(<ProjectSettings />, { wrapper: createWrapper() });

    const input = screen.getByPlaceholderText(/My Project/i);
    fireEvent.change(input, { target: { value: "New Project Name" } });
    
    const saveButton = screen.getByRole("button", { name: /Save Settings/i });
    fireEvent.click(saveButton);

    await waitFor(() => {
      expect(mockUpdateProjectSettings).toHaveBeenCalledWith("p1", {
        name: "New Project Name",
        notifications: {
          criticalityThreshold: null,
          volumeThreshold: null
        }
      });
      expect(toast.success).toHaveBeenCalledWith("Project settings updated");
    });
  });

  it("opens rotation dialog and triggers API key rotation", async () => {
    vi.mocked(rotateProjectApiKey).mockResolvedValueOnce("new-key-xyz");

    render(<ProjectSettings />, { wrapper: createWrapper() });

    fireEvent.click(screen.getByRole("button", { name: /Rotate API Key/i }));
    
    expect(screen.getByText(/Overlap Duration/i)).toBeInTheDocument();
    
    fireEvent.click(screen.getByRole("button", { name: /Start Rotation/i }));

    await waitFor(() => {
      expect(rotateProjectApiKey).toHaveBeenCalled();
      expect(screen.getByText(/New API Key Produced/i)).toBeInTheDocument();
      expect(screen.getByDisplayValue("new-key-xyz")).toBeInTheDocument();
    });
  });

  it("renders integration list and handles delete", async () => {
    const mockIntegrations = [
      { id: "int1", provider: "GitHub", name: "Test Repo" }
    ];
    vi.mocked(api.get).mockResolvedValue({ data: mockIntegrations });

    render(<ProjectSettings />, { wrapper: createWrapper() });

    expect(await screen.findByText("Test Repo")).toBeInTheDocument();
    expect(screen.getByText("GitHub Provider")).toBeInTheDocument();

    // Trigger delete
    window.confirm = vi.fn().mockReturnValue(true);
    fireEvent.click(screen.getByRole("button", { name: /Delete Integration/i })); 

    await waitFor(() => {
      expect(api.delete).toHaveBeenCalledWith("/integrations/int1");
    });
  });
});
