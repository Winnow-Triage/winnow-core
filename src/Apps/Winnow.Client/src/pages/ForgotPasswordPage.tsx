import { useState } from "react"
import { useNavigate } from "react-router-dom"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { ModeToggle } from "@/components/mode-toggle"
import { api } from "@/lib/api"
import { ArrowLeft } from "lucide-react"

export default function ForgotPasswordPage() {
    const navigate = useNavigate()
    const [isLoading, setIsLoading] = useState(false)
    const [error, setError] = useState<string | null>(null)
    const [success, setSuccess] = useState(false)

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault()
        setIsLoading(true)
        setError(null)

        const email = (document.getElementById('email') as HTMLInputElement).value;

        try {
            await api.post("/auth/forgot-password", { email });
            setSuccess(true);
        } catch (err: any) {
            console.error("Forgot Password Error:", err);
            setError(err.response?.data?.message || "Something went wrong. Please try again.");
        } finally {
            setIsLoading(false);
        }
    }

    return (
        <div className="min-h-screen w-full flex flex-col justify-center items-center p-4 md:p-8 bg-background relative">
            <div className="absolute top-4 right-4 md:top-8 md:right-8">
                <ModeToggle />
            </div>

            <div className="w-full max-w-md space-y-8 animate-in fade-in slide-in-from-bottom-4 duration-500">
                <div className="text-center space-y-2">
                    <h1 className="text-3xl font-bold tracking-tight">Forgot Password</h1>
                    <p className="text-muted-foreground">
                        Enter your email and we'll send you a link to reset your password.
                    </p>
                </div>

                {success ? (
                    <div className="space-y-6 text-center">
                        <div className="p-4 bg-blue-50 dark:bg-blue-900/20 text-blue-700 dark:text-blue-300 rounded-lg border border-blue-100 dark:border-blue-800">
                            Check your email! We've sent instructions to reset your password.
                        </div>
                        <Button variant="outline" className="w-full" onClick={() => navigate('/login')}>
                            Back to Login
                        </Button>
                    </div>
                ) : (
                    <form onSubmit={handleSubmit} className="space-y-4">
                        {error && (
                            <div className="p-3 text-sm text-red-500 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-900 rounded-md">
                                {error}
                            </div>
                        )}
                        <div className="space-y-2">
                            <Label htmlFor="email">Email</Label>
                            <Input id="email" type="email" placeholder="name@example.com" required />
                        </div>

                        <Button className="w-full" type="submit" disabled={isLoading}>
                            {isLoading ? "Sending..." : "Send Reset Link"}
                        </Button>

                        <Button
                            variant="ghost"
                            className="w-full gap-2 text-muted-foreground"
                            type="button"
                            onClick={() => navigate('/login')}
                        >
                            <ArrowLeft className="h-4 w-4" />
                            Back to Login
                        </Button>
                    </form>
                )}
            </div>
        </div>
    )
}
