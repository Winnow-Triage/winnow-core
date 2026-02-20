import { Lock } from "lucide-react"
import { useNavigate } from "react-router-dom"
import { Button } from "@/components/ui/button"

export default function SuspendedPage() {
    const navigate = useNavigate()

    return (
        <div className="min-h-screen w-full flex flex-col items-center justify-center p-4 bg-background">
            <div className="max-w-md w-full p-8 space-y-8 bg-card border border-border rounded-xl shadow-lg text-center animate-in fade-in zoom-in duration-500">
                <div className="mx-auto w-16 h-16 bg-red-500/10 rounded-full flex items-center justify-center mb-6">
                    <Lock className="w-8 h-8 text-red-500" />
                </div>

                <h1 className="text-3xl font-bold tracking-tight text-foreground">
                    Account Suspended
                </h1>

                <p className="text-muted-foreground">
                    Your organization's access to Winnow has been suspended by an administrator. All API requests and dashboard functions have been locked.
                </p>

                <div className="pt-6 border-t border-border mt-6">
                    <p className="text-sm text-muted-foreground mb-4">
                        If you believe this is an error or need to resolve an issue, please contact our support team.
                    </p>

                    <div className="flex flex-col sm:flex-row gap-3 justify-center">
                        <Button variant="outline" onClick={() => window.open('mailto:support@winnow.com')}>
                            Contact Support
                        </Button>
                        <Button onClick={() => navigate('/login')}>
                            Return to Login
                        </Button>
                    </div>
                </div>
            </div>
        </div>
    )
}
