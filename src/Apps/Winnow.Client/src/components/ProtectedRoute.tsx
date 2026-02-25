import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { ProjectProvider } from "../context/ProjectContext";
import { getMe } from "../lib/api";

interface ProtectedRouteProps {
    children: React.ReactNode;
}

export default function ProtectedRoute({ children }: ProtectedRouteProps) {
    const navigate = useNavigate();
    const [isLoading, setIsLoading] = useState(true);
    const [isAuthenticated, setIsAuthenticated] = useState(false);

    useEffect(() => {
        const checkAuth = async () => {
            try {
                const user = await getMe();
                // Update local storage if needed to keep it in sync
                localStorage.setItem("user", JSON.stringify({
                    id: user.id,
                    email: user.email,
                    name: user.fullName,
                    isEmailVerified: user.isEmailVerified,
                    defaultProjectId: user.defaultProjectId,
                    organizationId: user.activeOrganizationId
                }));
                setIsAuthenticated(true);
            } catch (error) {
                console.error("Auth check failed:", error);
                setIsAuthenticated(false);
                navigate("/login");
            } finally {
                setIsLoading(false);
            }
        };

        checkAuth();
    }, [navigate]);

    if (isLoading) {
        return (
            <div className="flex h-screen w-full items-center justify-center">
                <div className="h-8 w-8 animate-spin rounded-full border-4 border-primary border-t-transparent" />
            </div>
        );
    }

    if (!isAuthenticated) {
        return null;
    }

    // Wrap authenticated routes with ProjectProvider so the context is available
    return (
        <ProjectProvider>
            {children}
        </ProjectProvider>
    );
}
