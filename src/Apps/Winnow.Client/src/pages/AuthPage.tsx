import { useState, useEffect } from "react"
import { useLocation, useNavigate } from "react-router-dom"
import { Github } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { ModeToggle } from "@/components/mode-toggle"
import { api } from "@/lib/api"

export default function AuthPage() {
    const location = useLocation()
    const navigate = useNavigate()
    const [isSignUp, setIsSignUp] = useState(false)
    const [isLoading, setIsLoading] = useState(false)
    const [error, setError] = useState<string | null>(null)
    const [requiresOrgSelection, setRequiresOrgSelection] = useState(false)
    const [availableOrgs, setAvailableOrgs] = useState<any[]>([])
    const [selectedOrgId, setSelectedOrgId] = useState<string | null>(null)
    const [authPayload, setAuthPayload] = useState<any>(null)

    useEffect(() => {
        setIsSignUp(location.pathname === '/signup')
    }, [location.pathname])

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault()
        setIsLoading(true)
        setError(null)

        // Note: Inputs in the form need 'name' attributes for FormData to work
        const email = (document.getElementById('email') as HTMLInputElement).value;
        const password = (document.getElementById('password') as HTMLInputElement).value;
        const nameInput = document.getElementById('name') as HTMLInputElement;
        const fullName = nameInput ? nameInput.value : "";

        try {
            const endpoint = isSignUp ? "/auth/register" : "/auth/login";
            const payload = isSignUp
                ? { email, password, fullName }
                : { email, password, organizationId: selectedOrgId };

            let data;
            try {
                const response = await api.post(endpoint, payload);
                data = response.data;
            } catch (error: any) {
                const errorData = error.response?.data || {};
                let errorMessage = errorData.message || `Authentication failed: ${error.response?.statusText || error.message}`;

                // Parse ValidationProblemDetails 'errors' object
                if (errorData.errors) {
                    const validationErrors = Object.values(errorData.errors).flat();
                    if (validationErrors.length > 0) {
                        errorMessage = validationErrors.join(' ');
                    }
                }

                throw new Error(errorMessage);
            }

            // Check if organization selection is required
            if (data.requiresOrganizationSelection) {
                setRequiresOrgSelection(true);
                setAvailableOrgs(data.organizations);
                setAuthPayload({ email, password }); // Save for the next call
                return;
            }

            // Store Auth Data
            localStorage.removeItem("lastProjectId");
            localStorage.setItem("authToken", data.token);
            // Also keep 'user' for legacy/compatibility if needed, but store rich object
            localStorage.setItem("user", JSON.stringify({
                id: data.userId,
                email: data.email,
                name: data.fullName,
                defaultProjectId: data.defaultProjectId,
                organizationId: data.activeOrganizationId
            }));

            // Navigation
            if (isSignUp) {
                navigate('/setup', { state: { apiKey: data.apiKey } });
            } else {
                navigate('/dashboard');
            }

        } catch (err: any) {
            console.error("Auth Error:", err);
            setError(err.message || "Something went wrong. Please try again.");
        } finally {
            setIsLoading(false);
        }
    }

    const toggleMode = () => {
        if (isSignUp) {
            navigate('/login')
        } else {
            navigate('/signup')
        }
    }

    return (
        <div className="min-h-screen w-full flex">

            {/* Left Column: Branding (40%) - Hidden on mobile */}
            <div className="hidden lg:flex w-[40%] bg-gradient-to-br from-slate-900 via-blue-950 to-purple-950 flex-col justify-center p-12 relative overflow-hidden">
                {/* Background Pattern/Gradient Overlay */}
                <div className="absolute inset-0 bg-blue-500/10 mix-blend-overlay" />
                <div className="absolute top-0 right-0 w-96 h-96 bg-blue-500/20 rounded-full blur-3xl -translate-y-1/2 translate-x-1/2 opacity-50" />
                <div className="absolute bottom-0 left-0 w-96 h-96 bg-purple-500/20 rounded-full blur-3xl translate-y-1/2 -translate-x-1/2 opacity-50" />

                {/* Logo - Top Left */}
                <div className="absolute top-8 left-8 z-20">
                    <div className="flex items-center gap-2 font-bold text-2xl tracking-tight text-white/90">
                        <div className="relative flex h-8 w-8 items-center justify-center rounded-lg bg-blue-600 text-white shadow-lg shadow-blue-900/50">
                            <span className="font-mono text-lg">W</span>
                        </div>
                        Winnow
                    </div>
                </div>

                {/* Rotating Quote - Centered */}
                <div className="relative z-10 px-8 flex flex-col items-start min-h-[160px]">
                    <QuoteRotator />
                </div>

                {/* Attribution/Footer - Bottom Left */}
                <div className="absolute bottom-8 left-8 z-10">
                    <p className="text-sm font-medium text-blue-200/60 uppercase tracking-wider">
                        Trusted by developers who value their sanity.
                    </p>
                </div>
            </div>

            {/* Right Column: Form (60%) */}
            <div className="flex-1 flex flex-col justify-center items-center p-4 md:p-8 bg-background animate-in fade-in slide-in-from-right-4 duration-500 relative">

                {/* Theme Toggle */}
                <div className="absolute top-4 right-4 md:top-8 md:right-8">
                    <ModeToggle />
                </div>

                <div className="w-full max-w-md space-y-8">

                    <div className="text-center space-y-2">
                        <h1 className="text-3xl font-bold tracking-tight">
                            {isSignUp ? "Create your account." : "Welcome back."}
                        </h1>
                        <p className="text-muted-foreground">
                            {isSignUp ? "Start triaging at the speed of AI." : "Sign in to your account."}
                        </p>
                    </div>

                    {/* OAuth Buttons */}
                    <div className="grid grid-cols-2 gap-4">
                        <Button variant="outline" className="w-full text-md" type="button">
                            <Github className="mr-2 h-4 w-4" />
                            GitHub
                        </Button>
                        <Button variant="outline" className="w-full" type="button">
                            <svg className="mr-2 h-4 w-4" viewBox="0 0 24 24">
                                <path
                                    d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z"
                                    fill="#4285F4"
                                />
                                <path
                                    d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z"
                                    fill="#34A853"
                                />
                                <path
                                    d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z"
                                    fill="#FBBC05"
                                />
                                <path
                                    d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z"
                                    fill="#EA4335"
                                />
                            </svg>
                            Google
                        </Button>
                    </div>

                    <div className="relative">
                        <div className="absolute inset-0 flex items-center">
                            <span className="w-full border-t" />
                        </div>
                        <div className="relative flex justify-center text-xs uppercase">
                            <span className="bg-background px-2 text-muted-foreground">
                                Or continue with email
                            </span>
                        </div>
                    </div>

                    <form onSubmit={handleSubmit} className="space-y-4">
                        {requiresOrgSelection ? (
                            <div className="space-y-4 animate-in fade-in slide-in-from-bottom-2 duration-300">
                                <div className="text-sm font-medium">Select an organization to continue:</div>
                                <div className="grid gap-2">
                                    {availableOrgs.map((org) => (
                                        <Button
                                            key={org.id}
                                            variant={selectedOrgId === org.id ? "default" : "outline"}
                                            className="w-full justify-start text-left font-normal"
                                            onClick={() => setSelectedOrgId(org.id)}
                                            type="button"
                                        >
                                            <div className="flex flex-col items-start">
                                                <span>{org.name}</span>
                                            </div>
                                        </Button>
                                    ))}
                                </div>
                                <input type="hidden" id="email" value={authPayload?.email || ""} />
                                <input type="hidden" id="password" value={authPayload?.password || ""} />
                                <Button
                                    className="w-full mt-4"
                                    type="submit"
                                    disabled={!selectedOrgId || isLoading}
                                >
                                    {isLoading ? "Signing in..." : "Continue to Dashboard"}
                                </Button>
                                <Button
                                    variant="ghost"
                                    className="w-full text-xs"
                                    onClick={() => setRequiresOrgSelection(false)}
                                    type="button"
                                >
                                    Back to login
                                </Button>
                            </div>
                        ) : (
                            <>
                                {isSignUp && (
                                    <div className="space-y-2 animate-in fade-in slide-in-from-top-2 duration-300">
                                        <Label htmlFor="name">Full Name</Label>
                                        <Input id="name" placeholder="John Doe" required />
                                    </div>
                                )}

                                {error && (
                                    <div className="p-3 text-sm text-red-500 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-900 rounded-md animate-prev-slide-in-right">
                                        {error}
                                    </div>
                                )}
                                <div className="space-y-2">
                                    <Label htmlFor="email">Email</Label>
                                    <Input id="email" type="email" placeholder="name@example.com" required />
                                </div>
                                <div className="space-y-2">
                                    <Label htmlFor="password">Password</Label>
                                    <Input id="password" type="password" required />
                                    <div className="flex justify-end">
                                        <button
                                            type="button"
                                            onClick={() => navigate('/forgot-password')}
                                            className="text-xs text-muted-foreground hover:text-primary underline-offset-4 hover:underline"
                                        >
                                            Forgot password?
                                        </button>
                                    </div>
                                    {isSignUp && (
                                        <p className="text-xs text-muted-foreground">
                                            Minimum 6 characters. Requires upper & lower case letters, a digit, and a non-alphanumeric character (e.g., !, @, #).
                                        </p>
                                    )}
                                </div>

                                <Button
                                    className={`w-full text-md h-11 ${isSignUp ? 'bg-blue-600 hover:bg-blue-700 text-white' : ''}`}
                                    variant={isSignUp ? 'default' : 'default'} // Keeping default (dark) for Login, overriding class for SignUp
                                    type="submit"
                                    disabled={isLoading}
                                >
                                    {isLoading ? "Processing..." : (isSignUp ? "Get Started" : "Sign In")}
                                </Button>
                            </>
                        )}
                    </form>

                    <div className="text-center text-sm">
                        <button
                            onClick={toggleMode}
                            className="text-muted-foreground hover:text-primary underline-offset-4 hover:underline"
                        >
                            {isSignUp ? "Already have an account? Sign in." : "New to Winnow? Create an account."}
                        </button>
                    </div>
                </div>
            </div>
        </div>
    )
}

function QuoteRotator() {
    const quotes = [
        "Debug faster, sleep more.",
        "Triage at the speed of AI.",
        "Stop drowning in logs.",
        "From chaos to clarity."
    ]
    const [index, setIndex] = useState(0)
    const [isVisible, setIsVisible] = useState(true)

    useEffect(() => {
        const interval = setInterval(() => {
            setIsVisible(false) // Trigger exit
            setTimeout(() => {
                setIndex((prev) => (prev + 1) % quotes.length)
                setIsVisible(true) // Trigger enter
            }, 500) // Wait for exit transition
        }, 4000)

        return () => clearInterval(interval)
    }, [])

    return (
        <div className="h-32 flex items-center">
            <h2
                className={`text-5xl font-extrabold tracking-tight lg:text-6xl text-white transition-all duration-500 ease-in-out transform
                ${isVisible ? 'opacity-100 translate-y-0 blur-0' : 'opacity-0 -translate-y-4 blur-sm'}`}
                style={{ textShadow: '0 4px 20px rgba(0,0,0,0.3)' }}
            >
                "{quotes[index]}"
            </h2>
        </div>
    )
}
