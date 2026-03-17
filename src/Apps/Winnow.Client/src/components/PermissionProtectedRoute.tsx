import { Navigate } from "react-router-dom";
import { useAuth } from "@/context/AuthContext";
import React from "react";

interface PermissionProtectedRouteProps {
  children: React.ReactNode;
  permission: string;
}

export default function PermissionProtectedRoute({
  children,
  permission,
}: PermissionProtectedRouteProps) {
  const { user, isLoading, isInitialLoading } = useAuth();

  if (isLoading || isInitialLoading) {
    return (
      <div className="flex h-screen w-full items-center justify-center">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary"></div>
      </div>
    );
  }

  if (!user || !user.permissions?.includes(permission)) {
    // If not authorized, maybe drop them on the dashboard
    return <Navigate to="/dashboard" replace />;
  }

  return <>{children}</>;
}
