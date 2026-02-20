import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { parseJwt } from "@/lib/utils";

interface AdminProtectedRouteProps {
    children: React.ReactNode;
}

export default function AdminProtectedRoute({ children }: AdminProtectedRouteProps) {
    const navigate = useNavigate();
    const [isAuthorized, setIsAuthorized] = useState<boolean | null>(null);

    useEffect(() => {
        const token = localStorage.getItem("authToken");

        if (!token) {
            navigate("/login");
            return;
        }

        const decoded = parseJwt(token);
        if (!decoded) {
            localStorage.removeItem("authToken");
            navigate("/login");
            return;
        }

        const rolesClaim = decoded["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] || decoded.role;
        const roles = Array.isArray(rolesClaim) ? rolesClaim : [rolesClaim].filter(Boolean);

        if (roles.includes("SuperAdmin")) {
            setIsAuthorized(true);
        } else {
            navigate("/dashboard");
        }
    }, [navigate]);

    if (isAuthorized === null) {
        return null;
    }

    return <>{children}</>;
}
