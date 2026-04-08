import {
  useEffect,
  useState,
  useCallback,
  useRef
} from "react";
import { getMe, logoutUser as apiLogout } from "../lib/api";
import { isAxiosError } from "axios";

import type { User, LoginResponse } from "@/types";
import { AuthContext } from "@/hooks/use-auth";

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({
  children,
}) => {
  const [user, setUser] = useState<User | null>(() => {
    const saved = localStorage.getItem("user");
    return saved ? JSON.parse(saved) : null;
  });
  const [isLoading, setIsLoading] = useState(false);
  const [isInitialLoading, setIsInitialLoading] = useState(true);
  const [error, setError] = useState<Error | null>(null);

  const lastLoginTimestamp = useRef<number>(0);

  const refreshUser = useCallback(async () => {
    setIsLoading(true);
    const fetchStartTime = Date.now();
    try {
      const data = await getMe();
      const userData: User = {
        id: data.id,
        email: data.email,
        fullName: data.fullName,
        isEmailVerified: data.isEmailVerified,
        roles: data.roles,
        permissions: data.permissions || [],
        activeOrganizationId: data.activeOrganizationId,
        defaultProjectId: data.defaultProjectId,
        emailBounced: data.emailBounced,
      };
      setUser(userData);
      localStorage.setItem("user", JSON.stringify(userData));
      setError(null);
    } catch (err: unknown) {
      // If a login occurred after this request started, safely ignore the error
      // from the initial (now obsolete) /auth/me call to avoid logging the user out.
      if (lastLoginTimestamp.current > fetchStartTime) {
        return;
      }
      
      if (isAxiosError(err) && err.response?.status === 401) {
        // Initial check failed with 401, this is expected if not logged in
        setUser(null);
        localStorage.removeItem("user");
      } else if (isAxiosError(err) && (err.response?.status === 403 || err.response?.status === 429)) {
        // Ignore 403/429s for the initial check to prevent wipe/redirect loops
        // 403 usually means the org is suspended or user is restricted, but still "logged in"
      } else {
        console.error("Failed to fetch user:", err);
        setUser(null);
        localStorage.removeItem("user");
        setError(err instanceof Error ? err : new Error(String(err)));
      }
    } finally {
      setIsLoading(false);
      setIsInitialLoading(false);
    }
  }, []);

  useEffect(() => {
    refreshUser();
  }, [refreshUser]);

  const login = useCallback((userData: LoginResponse) => {
    lastLoginTimestamp.current = Date.now();
    const user: User = {
      id: userData.userId,
      email: userData.email,
      fullName: userData.fullName,
      isEmailVerified: userData.isEmailVerified,
      roles: userData.roles || [], // Login might not return roles if it only returns DTO
      permissions: userData.permissions || [],
      activeOrganizationId: userData.activeOrganizationId,
      defaultProjectId: userData.defaultProjectId,
      emailBounced: userData.emailBounced,
    };
    setUser(user);
    localStorage.setItem("user", JSON.stringify(user));
  }, []);

  const logout = useCallback(async () => {
    try {
      await apiLogout();
    } finally {
      setUser(null);
      localStorage.removeItem("user");
      localStorage.removeItem("lastProjectId");
    }
  }, []);

  return (
    <AuthContext.Provider
      value={{
        user,
        isAuthenticated: !!user,
        isLoading,
        isInitialLoading,
        error,
        login,
        logout,
        refreshUser,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
};
