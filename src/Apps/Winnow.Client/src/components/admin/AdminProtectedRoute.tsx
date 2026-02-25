import { useNavigate } from "react-router-dom";
import { useAuth } from "@/context/AuthContext";

interface AdminProtectedRouteProps {
    children: React.ReactNode;
}

export default function AdminProtectedRoute({ children }: AdminProtectedRouteProps) {
    const { user, isAuthenticated, isLoading } = useAuth();
    const navigate = useNavigate();

    if (isLoading) {
        return (
            <div className="flex h-screen w-full items-center justify-center">
                <div className="h-8 w-8 animate-spin rounded-full border-4 border-primary border-t-transparent" />
            </div>
        );
    }

    if (!isAuthenticated) {
        navigate("/login");
        return null;
    }

    if (!user?.roles.includes("SuperAdmin")) {
        navigate("/dashboard");
        return null;
    }

    return <>{children}</>;
}
