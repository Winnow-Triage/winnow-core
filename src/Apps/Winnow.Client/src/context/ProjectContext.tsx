import React, { createContext, useContext, useState, useEffect } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { api } from "../lib/api";

export interface Project {
    id: string;
    name: string;
    apiKey: string;
}

interface ProjectContextType {
    projects: Project[];
    currentProject: Project | null;
    isLoading: boolean;
    selectProject: (projectId: string) => void;
    refreshProjects: () => Promise<void>;
    createProject: (name: string) => Promise<Project>;
    renameProject: (id: string, newName: string) => Promise<void>;
    deleteProject: (id: string) => Promise<void>;
}

const ProjectContext = createContext<ProjectContextType | undefined>(undefined);

export function ProjectProvider({ children }: { children: React.ReactNode }) {
    const queryClient = useQueryClient();
    const [projects, setProjects] = useState<Project[]>([]);
    const [currentProject, setCurrentProject] = useState<Project | null>(null);
    const [isLoading, setIsLoading] = useState(true);

    useEffect(() => {
        refreshProjects();
    }, []);

    const refreshProjects = async () => {
        setIsLoading(true);
        const token = localStorage.getItem("authToken");

        if (!token) {
            setIsLoading(false);
            return;
        }

        try {
            const { data } = await api.get("/projects");
            setProjects(data);

            // Restore selection or default to first
            const savedProjectId = localStorage.getItem("lastProjectId");
            const matchedProject = data.find((p: Project) => p.id === savedProjectId) || data[0];

            if (matchedProject) {
                setCurrentProject(matchedProject);
                localStorage.setItem("lastProjectId", matchedProject.id);
            }
        } catch (error: any) {
            console.error("Failed to fetch projects", error);
            if (error.response?.status === 401) {
                // Token expired or invalid - handled by interceptor
            }
        } finally {
            setIsLoading(false);
        }
    };

    const selectProject = (projectId: string) => {
        const project = projects.find(p => p.id === projectId);
        if (project) {
            setCurrentProject(project);
            localStorage.setItem("lastProjectId", project.id);
            // Invalidate all queries to force refetch when project changes
            queryClient.invalidateQueries();
        }
    };

    const createProject = async (name: string): Promise<Project> => {
        const token = localStorage.getItem("authToken");
        if (!token) throw new Error("Not authenticated");

        try {
            const { data: newProject } = await api.post("/projects", { name });
            setProjects(prev => [...prev, newProject]);
            selectProject(newProject.id); // Auto-select new project
            return newProject;
        } catch (error: any) {
            console.error("Create project error", error);
            throw new Error(error.response?.data?.message || "Failed to create project");
        }
    };

    const renameProject = async (id: string, newName: string) => {
        const token = localStorage.getItem("authToken");
        if (!token) return;

        try {
            await api.put(`/projects/${id}`, { name: newName });

            // Update local state without full refetch
            setProjects(prev => prev.map(p => p.id === id ? { ...p, name: newName } : p));
            if (currentProject?.id === id) {
                setCurrentProject(prev => prev ? { ...prev, name: newName } : null);
            }
        } catch (error: any) {
            console.error("Rename project error", error);
            throw new Error(error.response?.data?.message || "Failed to rename project");
        }
    };

    const deleteProject = async (id: string) => {
        const token = localStorage.getItem("authToken");
        if (!token) return;

        try {
            await api.delete(`/projects/${id}`);

            setProjects(prev => {
                const updated = prev.filter(p => p.id !== id);

                // If we deleted the current project, auto-select a new one
                if (currentProject?.id === id) {
                    if (updated.length > 0) {
                        selectProject(updated[0].id);
                    } else {
                        setCurrentProject(null);
                        localStorage.removeItem("lastProjectId");
                        queryClient.invalidateQueries();
                    }
                }

                return updated;
            });
        } catch (error: any) {
            console.error("Delete project error", error);
            throw new Error(error.response?.data?.message || "Failed to delete project");
        }
    };

    return (
        <ProjectContext.Provider value={{ projects, currentProject, isLoading, selectProject, refreshProjects, createProject, renameProject, deleteProject }}>
            {children}
        </ProjectContext.Provider>
    );
}

export function useProject() {
    const context = useContext(ProjectContext);
    if (context === undefined) {
        throw new Error("useProject must be used within a ProjectProvider");
    }
    return context;
}
