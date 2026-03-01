import { useNavigate } from "react-router-dom";
import React, { useEffect } from "react";
import { ProjectProvider } from "../context/ProjectContext";
import { useAuth } from "../context/AuthContext";

interface ProtectedRouteProps {
    children: React.ReactNode;
}

export default function ProtectedRoute({ children }: ProtectedRouteProps) {
    const { isAuthenticated, isLoading, isInitialLoading } = useAuth();
    const navigate = useNavigate();

    useEffect(() => {
        if (!isInitialLoading && !isLoading && !isAuthenticated) {
            navigate("/login");
        }
    }, [isInitialLoading, isLoading, isAuthenticated, navigate]);

    if (isInitialLoading || isLoading) {
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
