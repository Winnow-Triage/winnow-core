import React from "react";
import { useAuth } from "@/hooks/use-auth";

interface PermissionGateProps {
  children: React.ReactNode;
  /**
   * The name of the permission required to render the children.
   * e.g., "users:manage", "billing:read"
   */
  permission: string;
  /**
   * Optional fallback to render when the user does not have permission.
   */
  fallback?: React.ReactNode;
}

export function PermissionGate({
  children,
  permission,
  fallback = null,
}: PermissionGateProps) {
  const { user } = useAuth();

  // If there's no user, or they don't have the permissions array, deny access
  if (!user || !user.permissions) {
    return <>{fallback}</>;
  }

  // Check if the user's permissions array includes the required permission
  if (user.permissions.includes(permission)) {
    return <>{children}</>;
  }

  // User lacks permission
  return <>{fallback}</>;
}
