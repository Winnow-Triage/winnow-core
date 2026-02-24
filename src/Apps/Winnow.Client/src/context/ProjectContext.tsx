import React, { createContext, useContext, useState, useEffect } from "react";
import { useQueryClient, useQuery } from "@tanstack/react-query";
import { api } from "../lib/api";

export interface Project {
    id: string;
    name: string;
    apiKey: string;
    teamId?: string | null;
}

interface ProjectContextType {
    projects: Project[];
    currentProject: Project | null;
    isLoading: boolean;
    orgWide: boolean;
    setOrgWide: (value: boolean) => void;
    selectProject: (projectId: string) => void;
    refreshProjects: () => Promise<void>;
    createProject: (name: string) => Promise<Project>;
    renameProject: (id: string, newName: string) => Promise<void>;
    deleteProject: (id: string) => Promise<void>;
}

const ProjectContext = createContext<ProjectContextType | undefined>(undefined);

export function ProjectProvider({ children }: { children: React.ReactNode }) {
    const queryClient = useQueryClient();
    const [currentProject, setCurrentProject] = useState<Project | null>(null);
    const [orgWide, setOrgWide] = useState(false);

    const { data: projects = [], isLoading } = useQuery<Project[]>({
        queryKey: ['projects', orgWide],
        queryFn: async ({ queryKey }) => {
            const [_, isOrgWide] = queryKey;
            const token = localStorage.getItem("authToken");
            if (!token) return [];
            const { data } = await api.get("/projects", { params: { orgWide: isOrgWide } });
            return data;
        },
        // We set the current project once the initial load is done if not already set
        meta: {
            onSuccess: (data: Project[]) => {
                if (!currentProject && data.length > 0) {
                    const savedProjectId = localStorage.getItem("lastProjectId");
                    const matchedProject = data.find((p: Project) => p.id === savedProjectId) || data[0];
                    if (matchedProject) {
                        setCurrentProject(matchedProject);
                        localStorage.setItem("lastProjectId", matchedProject.id);
                    }
                }
            }
        }
    });

    // Handle initial selection recovery manually because meta.onSuccess is deprecated/tricky in v5
    useEffect(() => {
        if (!isLoading && projects.length > 0 && !currentProject) {
            const savedProjectId = localStorage.getItem("lastProjectId");
            const matchedProject = projects.find((p: Project) => p.id === savedProjectId) || projects[0];
            if (matchedProject) {
                setCurrentProject(matchedProject);
                localStorage.setItem("lastProjectId", matchedProject.id);
            }
        }
    }, [projects, isLoading, currentProject]);

    const refreshProjects = async () => {
        await queryClient.invalidateQueries({ queryKey: ['projects'] });
    };

    const selectProject = (projectId: string) => {
        const project = projects.find((p: Project) => p.id === projectId);
        if (project) {
            setCurrentProject(project);
            localStorage.setItem("lastProjectId", project.id);
            // Invalidate all queries to force refetch when project changes
            queryClient.invalidateQueries();
        }
    };

    const createProject = async (name: string): Promise<Project> => {
        try {
            const { data: newProject } = await api.post("/projects", { name });
            await refreshProjects();
            selectProject(newProject.id); // Auto-select new project
            return newProject;
        } catch (error: any) {
            console.error("Create project error", error);
            throw new Error(error.response?.data?.message || "Failed to create project");
        }
    };

    const renameProject = async (id: string, newName: string) => {
        try {
            await api.put(`/projects/${id}`, { name: newName });
            await refreshProjects();
            if (currentProject?.id === id) {
                setCurrentProject(prev => prev ? { ...prev, name: newName } : null);
            }
        } catch (error: any) {
            console.error("Rename project error", error);
            throw new Error(error.response?.data?.message || "Failed to rename project");
        }
    };

    const deleteProject = async (id: string) => {
        try {
            await api.delete(`/projects/${id}`);
            await refreshProjects();

            if (currentProject?.id === id) {
                // The query will have updated the projects list already due to invalidate
                // but we need to pick a new one for currentProject
                const nextProject = projects.find((p: Project) => p.id !== id);
                if (nextProject) {
                    selectProject(nextProject.id);
                } else {
                    setCurrentProject(null);
                    localStorage.removeItem("lastProjectId");
                    queryClient.invalidateQueries();
                }
            }
        } catch (error: any) {
            console.error("Delete project error", error);
            throw new Error(error.response?.data?.message || "Failed to delete project");
        }
    };

    return (
        <ProjectContext.Provider value={{ projects, currentProject, isLoading, orgWide, setOrgWide, selectProject, refreshProjects, createProject, renameProject, deleteProject }}>
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
