import React, { createContext, useContext, useEffect, useState, useCallback } from "react";
import { getMe, logoutUser as apiLogout } from "../lib/api";
import { isAxiosError } from "axios";

interface User {
    id: string;
    email: string;
    fullName: string;
    isEmailVerified: boolean;
    roles: string[];
    activeOrganizationId?: string;
    defaultProjectId?: string;
}

interface AuthContextType {
    user: User | null;
    isAuthenticated: boolean;
    isLoading: boolean;
    error: any;
    login: (userData: any) => void;
    logout: () => Promise<void>;
    refreshUser: () => Promise<void>;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
    const [user, setUser] = useState<User | null>(() => {
        const saved = localStorage.getItem("user");
        return saved ? JSON.parse(saved) : null;
    });
    const [isLoading, setIsLoading] = useState(true);
    const [error, setError] = useState<any>(null);

    const refreshUser = useCallback(async () => {
        try {
            const data = await getMe();
            const userData: User = {
                id: data.id,
                email: data.email,
                fullName: data.fullName,
                isEmailVerified: data.isEmailVerified,
                roles: data.roles,
                activeOrganizationId: data.activeOrganizationId,
                defaultProjectId: data.defaultProjectId
            };
            setUser(userData);
            localStorage.setItem("user", JSON.stringify(userData));
            setError(null);
        } catch (err) {
            console.error("Failed to fetch user:", err);
            if (isAxiosError(err) && err.response?.status === 429) {
                // Ignore 429s, keep current user if exists
            } else {
                setUser(null);
                localStorage.removeItem("user");
                setError(err);
            }
        } finally {
            setIsLoading(false);
        }
    }, []);

    useEffect(() => {
        refreshUser();
    }, [refreshUser]);

    const login = useCallback((userData: any) => {
        const user: User = {
            id: userData.userId,
            email: userData.email,
            fullName: userData.fullName,
            isEmailVerified: userData.isEmailVerified,
            roles: userData.roles || [], // Login might not return roles if it only returns DTO
            activeOrganizationId: userData.activeOrganizationId,
            defaultProjectId: userData.defaultProjectId
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
                error,
                login,
                logout,
                refreshUser
            }}
        >
            {children}
        </AuthContext.Provider>
    );
};

export const useAuth = () => {
    const context = useContext(AuthContext);
    if (context === undefined) {
        throw new Error("useAuth must be used within an AuthProvider");
    }
    return context;
};
