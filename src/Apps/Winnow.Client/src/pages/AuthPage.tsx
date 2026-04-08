import { useState, useEffect } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { ModeToggle } from "@/components/mode-toggle";
import { api } from "@/lib/api";
import { validatePassword } from "@/lib/auth-utils";
import { useAuth } from "@/hooks/use-auth";
import type { Organization } from "@/types";

// Auth Sub-components
import { AuthBrandingPanel } from "@/components/auth/AuthBrandingPanel";
import { SocialAuth } from "@/components/auth/SocialAuth";
import { LoginForm } from "@/components/auth/LoginForm";
import { SignUpForm } from "@/components/auth/SignUpForm";
import { OrgSelectionForm } from "@/components/auth/OrgSelectionForm";
import { DemoLogin } from "@/components/auth/DemoLogin";

export default function AuthPage() {
  const location = useLocation();
  const navigate = useNavigate();
  const { login, isAuthenticated, isInitialLoading } = useAuth();
  
  // UI State
  const [isSignUp, setIsSignUp] = useState(false);
  const [justSubmitted, setJustSubmitted] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  
  // Form State
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [agreedToTerms, setAgreedToTerms] = useState(false);
  
  // Org Selection State
  const [requiresOrgSelection, setRequiresOrgSelection] = useState(false);
  const [availableOrgs, setAvailableOrgs] = useState<Pick<Organization, "id" | "name">[]>([]);
  const [selectedOrgId, setSelectedOrgId] = useState<string | null>(null);

  // Auto-redirect if already logged in
  useEffect(() => {
    if (isAuthenticated && !isInitialLoading && !justSubmitted) {
      navigate("/dashboard");
    }
  }, [isAuthenticated, isInitialLoading, navigate, justSubmitted]);

  // Sync mode based on URL
  useEffect(() => {
    setIsSignUp(location.pathname === "/signup");
  }, [location.pathname]);

  // Demo mode defaults
  useEffect(() => {
    if (import.meta.env.VITE_DEMO_MODE === "true" && !isSignUp) {
      setEmail("demo@winnowtriage.com");
      setPassword("demo");
    }
  }, [isSignUp]);

  const handleDemoLogin = async () => {
    setIsLoading(true);
    try {
      const response = await api.post("/auth/login", {
        email: "demo@winnowtriage.com",
        password: "demo",
        organizationId: "demo-org-alpha",
      });
      login(response.data);
      navigate("/");
    } catch {
      setError("Demo login failed.");
      setIsLoading(false);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsLoading(true);
    setError(null);

    // Get name from DOM (since it's in the sub-component)
    const nameInput = document.getElementById("name") as HTMLInputElement;
    const fullName = nameInput ? nameInput.value : "";

    if (isSignUp) {
      if (!agreedToTerms) {
        setError("You must agree to the Terms of Service to create an account.");
        setIsLoading(false);
        return;
      }
      if (!validatePassword(password)) {
        setError("Please ensure your password meets all requirements.");
        setIsLoading(false);
        return;
      }
      if (password !== confirmPassword) {
        setError("Passwords do not match.");
        setIsLoading(false);
        return;
      }
    }

    try {
      const endpoint = isSignUp ? "/auth/register" : "/auth/login";
      const payload = isSignUp
        ? { email, password, fullName }
        : { email, password, organizationId: selectedOrgId };

      let data;
      try {
        const response = await api.post(endpoint, payload);
        data = response.data;
      } catch (err: unknown) {
        const e = err as { response?: { data?: { message?: string; errors?: Record<string, string[]> } } };
        const errorData = e.response?.data || {};
        let errorMessage = errorData.message || "Authentication failed";

        if (errorData.errors) {
          const validationErrors = Object.values(errorData.errors).flat();
          if (validationErrors.length > 0) {
            errorMessage = validationErrors.join(" ");
          }
        }
        throw new Error(errorMessage);
      }

      if (data.requiresOrganizationSelection) {
        setRequiresOrgSelection(true);
        setAvailableOrgs(data.organizations);
        return;
      }

      setJustSubmitted(true);
      login(data);

      if (isSignUp) {
        navigate("/setup", { state: { apiKey: data?.apiKey || "fake-key" } });
      } else {
        window.location.href = "/dashboard";
      }
    } catch (err: unknown) {
      const e = err as { response?: { data?: { message?: string } }; message?: string };
      setError(e.response?.data?.message || e.message || "Something went wrong. Please try again.");
    } finally {
      setIsLoading(false);
    }
  };

  const toggleMode = () => {
    navigate(isSignUp ? "/login" : "/signup");
  };

  return (
    <div className="min-h-screen w-full flex">
      <AuthBrandingPanel />

      <div className="flex-1 flex flex-col justify-center items-center p-4 md:p-8 bg-background animate-in fade-in slide-in-from-right-4 duration-500 relative">
        <div className="absolute top-4 right-4 md:top-8 md:right-8">
          <ModeToggle />
        </div>

        <div className="w-full max-w-md space-y-8">
          <div className="text-center space-y-2">
            <h1 className="text-3xl font-bold tracking-tight">
              {import.meta.env.VITE_DEMO_MODE === "true"
                ? "Winnow Sandbox"
                : isSignUp ? "Create your account." : "Welcome back."}
            </h1>
            <p className="text-muted-foreground">
              {import.meta.env.VITE_DEMO_MODE === "true"
                ? "Experience the full power of Winnow in a simulated environment."
                : isSignUp ? "Start triaging at the speed of AI." : "Sign in to your account."}
            </p>
          </div>

          {import.meta.env.VITE_DEMO_MODE !== "true" && <SocialAuth />}

          {import.meta.env.VITE_DEMO_MODE !== "true" && (
            <form onSubmit={handleSubmit} className="space-y-4">
              {error && (
                <div className="p-3 text-sm text-red-500 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-900 rounded-md animate-prev-slide-in-right">
                  {error}
                </div>
              )}

              {requiresOrgSelection ? (
                <OrgSelectionForm
                  availableOrgs={availableOrgs}
                  selectedOrgId={selectedOrgId}
                  setSelectedOrgId={setSelectedOrgId}
                  isLoading={isLoading}
                  onBack={() => setRequiresOrgSelection(false)}
                />
              ) : isSignUp ? (
                <SignUpForm
                  email={email}
                  setEmail={setEmail}
                  password={password}
                  setPassword={setPassword}
                  confirmPassword={confirmPassword}
                  setConfirmPassword={setConfirmPassword}
                  agreedToTerms={agreedToTerms}
                  setAgreedToTerms={setAgreedToTerms}
                  isLoading={isLoading}
                />
              ) : (
                <LoginForm
                  email={email}
                  setEmail={setEmail}
                  password={password}
                  setPassword={setPassword}
                  isLoading={isLoading}
                />
              )}
            </form>
          )}

          {import.meta.env.VITE_DEMO_MODE === "true" && (
            <DemoLogin isLoading={isLoading} onLogin={handleDemoLogin} />
          )}

          {import.meta.env.VITE_DEMO_MODE !== "true" && (
            <div className="text-center text-sm">
              <button
                onClick={toggleMode}
                className="text-muted-foreground hover:text-primary underline-offset-4 hover:underline"
              >
                {isSignUp ? "Already have an account? Sign in." : "New to Winnow? Create an account."}
              </button>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
