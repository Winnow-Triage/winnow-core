import { useState, useEffect } from "react"
import { useNavigate, useSearchParams } from "react-router-dom"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { ModeToggle } from "@/components/mode-toggle"
import { api } from "@/lib/api"
import { PasswordRules, validatePassword } from "@/components/PasswordRules"

export default function ResetPasswordPage() {
    const navigate = useNavigate()
    const [searchParams] = useSearchParams()
    const [isLoading, setIsLoading] = useState(false)
    const [error, setError] = useState<string | null>(null)
    const [success, setSuccess] = useState(false)
    const [newPassword, setNewPassword] = useState("")
    const [confirmPassword, setConfirmPassword] = useState("")

    const email = searchParams.get('email') || ""
    const token = searchParams.get('token') || ""

    useEffect(() => {
        if (!email || !token) {
            setError("Invalid or missing reset token. Please request a new link.");
        }
    }, [email, token])

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault()
        setIsLoading(true)
        setError(null)

        if (!validatePassword(newPassword)) {
            setError("Please ensure your password meets all requirements.");
            setIsLoading(false);
            return;
        }

        if (newPassword !== confirmPassword) {
            setError("Passwords do not match.");
            setIsLoading(false);
            return;
        }

        try {
            await api.post("/auth/reset-password", { email, token, newPassword });
            setSuccess(true);
        } catch (err: any) {
            console.error("Reset Password Error:", err);
            const errorData = err.response?.data || {};
            let errorMessage = errorData.message || "Failed to reset password.";

            if (errorData.errors) {
                errorMessage = Object.values(errorData.errors).flat().join(' ');
            }

            setError(errorMessage);
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
                    <h1 className="text-3xl font-bold tracking-tight">Set New Password</h1>
                    <p className="text-muted-foreground">
                        Please enter your new password below.
                    </p>
                </div>

                {success ? (
                    <div className="space-y-6 text-center">
                        <div className="p-4 bg-green-50 dark:bg-green-900/20 text-green-700 dark:text-green-300 rounded-lg border border-green-100 dark:border-green-800">
                            Password reset successfully! You can now sign in with your new password.
                        </div>
                        <Button className="w-full" onClick={() => navigate('/login')}>
                            Go to Login
                        </Button>
                    </div>
                ) : (
                    <form onSubmit={handleSubmit} className="space-y-4">
                        {error && (
                            <div className="p-3 text-sm text-red-500 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-900 rounded-md">
                                {error}
                            </div>
                        )}

                        <div className="space-y-2 text-sm text-muted-foreground">
                            Resetting password for: <span className="font-semibold text-foreground">{email}</span>
                        </div>

                        <div className="space-y-2">
                            <Label htmlFor="password">New Password</Label>
                            <Input
                                id="password"
                                type="password"
                                required
                                disabled={!email || !token}
                                value={newPassword}
                                onChange={(e) => setNewPassword(e.target.value)}
                            />
                            <PasswordRules password={newPassword} className="mt-2" />
                        </div>

                        <div className="space-y-2">
                            <Label htmlFor="confirm-password">Confirm New Password</Label>
                            <Input
                                id="confirm-password"
                                type="password"
                                required
                                disabled={!email || !token}
                                value={confirmPassword}
                                onChange={(e) => setConfirmPassword(e.target.value)}
                            />
                        </div>

                        <Button className="w-full" type="submit" disabled={isLoading || !email || !token}>
                            {isLoading ? "Resetting..." : "Reset Password"}
                        </Button>
                    </form>
                )}
            </div>
        </div>
    )
}
