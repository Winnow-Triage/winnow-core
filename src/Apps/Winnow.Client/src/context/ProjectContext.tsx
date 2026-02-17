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
    createProject: (name: string) => Promise<void>;
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

    const createProject = async (name: string) => {
        const token = localStorage.getItem("authToken");
        if (!token) return;

        try {
            const { data: newProject } = await api.post("/projects", { name });
            setProjects(prev => [...prev, newProject]);
            selectProject(newProject.id); // Auto-select new project
        } catch (error: any) {
            console.error("Create project error", error);
            throw new Error(error.response?.data?.message || "Failed to create project");
        }
    };

    return (
        <ProjectContext.Provider value={{ projects, currentProject, isLoading, selectProject, refreshProjects, createProject }}>
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
