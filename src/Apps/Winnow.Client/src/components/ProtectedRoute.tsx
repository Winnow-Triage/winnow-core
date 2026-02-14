import { useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { ProjectProvider } from "../context/ProjectContext";

interface ProtectedRouteProps {
    children: React.ReactNode;
}

export default function ProtectedRoute({ children }: ProtectedRouteProps) {
    const navigate = useNavigate();
    const token = localStorage.getItem("authToken");

    useEffect(() => {
        if (!token) {
            navigate("/login");
        }
    }, [token, navigate]);

    if (!token) {
        return null; // Or a loading spinner
    }

    // Wrap authenticated routes with ProjectProvider so the context is available
    return (
        <ProjectProvider>
            {children}
        </ProjectProvider>
    );
}
