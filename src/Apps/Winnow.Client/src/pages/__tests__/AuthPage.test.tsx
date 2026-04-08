import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import "@testing-library/jest-dom";
import AuthPage from "../AuthPage";
import { vi } from "vitest";
import { BrowserRouter } from "react-router-dom";
import { api } from "@/lib/api";
import { useAuth } from "@/hooks/use-auth";

// Mock dependencies
vi.mock("react-markdown", () => ({
  default: ({ children }: { children: React.ReactNode }) => <div>{children}</div>,
}));

vi.mock("@/lib/api", () => ({
  api: {
    post: vi.fn(),
    get: vi.fn(),
  },
}));

vi.mock("@/hooks/use-auth", () => ({
  useAuth: vi.fn(),
}));

const mockNavigate = vi.fn();
vi.mock("react-router-dom", async () => {
  const actual = await vi.importActual("react-router-dom");
  return {
    ...actual,
    useNavigate: () => mockNavigate,
    useLocation: () => ({ pathname: "/login" }),
  };
});

describe("AuthPage Component", () => {
  const mockLogin = vi.fn();

  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(useAuth).mockReturnValue({
      login: mockLogin,
      user: null,
      isAuthenticated: false,
      isLoading: false,
      isInitialLoading: false,
      error: null,
      logout: vi.fn(),
      refreshUser: vi.fn(),
    });
  });

  it("renders login form by default", () => {
    render(
      <BrowserRouter>
        <AuthPage />
      </BrowserRouter>
    );
    expect(screen.getByText(/Welcome back./i)).toBeInTheDocument();
    expect(screen.getByLabelText(/Email/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/Password/i)).toBeInTheDocument();
  });

  it("handles successful login", async () => {
    vi.mocked(api.post).mockResolvedValue({ data: { userId: "1", fullName: "Test User" } });
    
    // Switch window.location behavior for test
    const originalLocation = window.location;
    // Using a more structured approach than 'as any'
    const windowSpy = window as unknown as { location: Partial<Location> };
    delete (windowSpy as unknown as { location: unknown }).location;
    windowSpy.location = { ...originalLocation, href: "" };

    render(
      <BrowserRouter>
        <AuthPage />
      </BrowserRouter>
    );

    fireEvent.change(screen.getByLabelText(/Email/i), { target: { value: "test@example.com" } });
    fireEvent.change(screen.getByLabelText(/Password/i), { target: { value: "password123" } });
    fireEvent.click(screen.getByRole("button", { name: /Sign In/i }));

    await waitFor(() => {
      expect(api.post).toHaveBeenCalledWith("/auth/login", expect.objectContaining({
        email: "test@example.com",
        password: "password123"
      }));
      expect(mockLogin).toHaveBeenCalled();
      expect(window.location.href).toBe("/dashboard");
    });

    (window as unknown as { location: Location }).location = originalLocation;
  });

  it("shows error message on failed login", async () => {
    vi.mocked(api.post).mockRejectedValue({
      response: { data: { message: "Invalid credentials" } }
    });

    render(
      <BrowserRouter>
        <AuthPage />
      </BrowserRouter>
    );

    fireEvent.change(screen.getByLabelText(/Email/i), { target: { value: "wrong@example.com" } });
    fireEvent.change(screen.getByLabelText(/Password/i), { target: { value: "wrong" } });
    fireEvent.click(screen.getByRole("button", { name: /Sign In/i }));

    expect(await screen.findByText(/Invalid credentials/i)).toBeInTheDocument();
  });

  it("handles organization selection if required", async () => {
    vi.mocked(api.post).mockResolvedValueOnce({
      data: {
        requiresOrganizationSelection: true,
        organizations: [
          { id: "org-1", name: "Org One" },
          { id: "org-2", name: "Org Two" }
        ]
      }
    });

    render(
      <BrowserRouter>
        <AuthPage />
      </BrowserRouter>
    );

    fireEvent.change(screen.getByLabelText(/Email/i), { target: { value: "multi@example.com" } });
    fireEvent.change(screen.getByLabelText(/Password/i), { target: { value: "password" } });
    fireEvent.click(screen.getByRole("button", { name: /Sign In/i }));

    expect(await screen.findByText(/Select an organization to continue:/i)).toBeInTheDocument();
    expect(screen.getByText("Org One")).toBeInTheDocument();
    expect(screen.getByText("Org Two")).toBeInTheDocument();

    // Select an org and continue
    vi.mocked(api.post).mockResolvedValueOnce({ data: { userId: "1" } });
    fireEvent.click(screen.getByText("Org One"));
    fireEvent.click(screen.getByRole("button", { name: /Continue to Dashboard/i }));

    await waitFor(() => {
      expect(api.post).toHaveBeenLastCalledWith("/auth/login", expect.objectContaining({
        organizationId: "org-1"
      }));
    });
  });
});
