import { useState, useEffect, useRef } from "react"
import { useNavigate, useSearchParams } from "react-router-dom"
import { Button } from "@/components/ui/button"
import { ModeToggle } from "@/components/mode-toggle"
import { api } from "@/lib/api"
import { CheckCircle2, XCircle, Loader2 } from "lucide-react"

export default function VerifyEmailPage() {
    const navigate = useNavigate()
    const [searchParams] = useSearchParams()
    const [status, setStatus] = useState<'loading' | 'success' | 'error'>('loading')
    const [errorMessage, setErrorMessage] = useState<string | null>(null)
    const verificationStarted = useRef(false)

    const userId = searchParams.get('userId')
    const token = searchParams.get('token')

    useEffect(() => {
        const verifyEmail = async () => {
            if (verificationStarted.current) return;
            verificationStarted.current = true;

            if (!userId || !token) {
                setStatus('error');
                setErrorMessage("Invalid verification link. Missing user information or token.");
                return;
            }

            try {
                // GET request as defined in VerifyEmailEndpoint
                await api.get(`/auth/verify-email?userId=${userId}&token=${encodeURIComponent(token)}`);
                setStatus('success');
            } catch (err: any) {
                console.error("Email Verification Error:", err);
                const errorData = err.response?.data || {};
                let message = errorData.message || "Email verification failed.";

                if (errorData.errors) {
                    message = Object.values(errorData.errors).flat().join(' ');
                }

                setErrorMessage(message);
                setStatus('error');
            }
        };

        verifyEmail();
    }, [userId, token])

    return (
        <div className="min-h-screen w-full flex flex-col justify-center items-center p-4 md:p-8 bg-background relative">
            <div className="absolute top-4 right-4 md:top-8 md:right-8">
                <ModeToggle />
            </div>

            <div className="w-full max-w-md space-y-8 text-center animate-in fade-in slide-in-from-bottom-4 duration-500">
                <div className="space-y-2">
                    <h1 className="text-3xl font-bold tracking-tight">Email Verification</h1>
                    <p className="text-muted-foreground">
                        {status === 'loading' && "Please wait while we verify your email..."}
                        {status === 'success' && "Your email has been successfully verified."}
                        {status === 'error' && "We couldn't verify your email address."}
                    </p>
                </div>

                <div className="flex justify-center py-4">
                    {status === 'loading' && (
                        <Loader2 className="h-16 w-16 text-primary animate-spin" />
                    )}
                    {status === 'success' && (
                        <CheckCircle2 className="h-16 w-16 text-green-500" />
                    )}
                    {status === 'error' && (
                        <XCircle className="h-16 w-16 text-destructive" />
                    )}
                </div>

                {status === 'error' && errorMessage && (
                    <div className="p-4 bg-destructive/10 text-destructive rounded-lg border border-destructive/20 text-sm">
                        {errorMessage}
                    </div>
                )}

                {status !== 'loading' && (
                    <div className="space-y-4 pt-4">
                        <Button className="w-full" onClick={() => navigate('/login')}>
                            {status === 'success' ? "Sign In" : "Back to Login"}
                        </Button>
                    </div>
                )}
            </div>
        </div>
    )
}
