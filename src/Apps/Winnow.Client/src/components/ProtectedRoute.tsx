import { useNavigate } from "react-router-dom";
import { ProjectProvider } from "../context/ProjectContext";
import { useAuth } from "../context/AuthContext";

interface ProtectedRouteProps {
    children: React.ReactNode;
}

export default function ProtectedRoute({ children }: ProtectedRouteProps) {
    const { isAuthenticated, isLoading } = useAuth();
    const navigate = useNavigate();

    // No useEffect needed here as AuthProvider handles the initial check

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

    // Wrap authenticated routes with ProjectProvider so the context is available
    return (
        <ProjectProvider>
            {children}
        </ProjectProvider>
    );
}
