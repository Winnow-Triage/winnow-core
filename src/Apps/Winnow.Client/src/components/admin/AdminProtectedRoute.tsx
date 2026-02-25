import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { getMe } from "@/lib/api";

interface AdminProtectedRouteProps {
    children: React.ReactNode;
}

export default function AdminProtectedRoute({ children }: AdminProtectedRouteProps) {
    const navigate = useNavigate();
    const [isLoading, setIsLoading] = useState(true);
    const [isAuthorized, setIsAuthorized] = useState<boolean>(false);

    useEffect(() => {
        const checkAdmin = async () => {
            try {
                const user = await getMe();
                if (user.roles.includes("SuperAdmin")) {
                    setIsAuthorized(true);
                } else {
                    navigate("/dashboard");
                }
            } catch (error) {
                console.error("Admin auth check failed:", error);
                navigate("/login");
            } finally {
                setIsLoading(false);
            }
        };

        checkAdmin();
    }, [navigate]);

    if (isLoading) {
        return (
            <div className="flex h-screen w-full items-center justify-center">
                <div className="h-8 w-8 animate-spin rounded-full border-4 border-primary border-t-transparent" />
            </div>
        );
    }

    if (!isAuthorized) {
        return null;
    }

    if (isAuthorized === null) {
        return null;
    }

    return <>{children}</>;
}
