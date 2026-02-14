import React, { createContext, useContext, useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";

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
    const [projects, setProjects] = useState<Project[]>([]);
    const [currentProject, setCurrentProject] = useState<Project | null>(null);
    const [isLoading, setIsLoading] = useState(true);
    const navigate = useNavigate();

    const API_URL = "http://localhost:5294"; // In real app, from env

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
            const response = await fetch(`${API_URL}/projects`, {
                headers: {
                    "Authorization": `Bearer ${token}`
                }
            });

            if (response.status === 401) {
                // Token expired or invalid
                localStorage.removeItem("authToken");
                navigate("/login");
                return;
            }

            if (response.ok) {
                const data = await response.json();
                setProjects(data);

                // Restore selection or default to first
                const savedProjectId = localStorage.getItem("lastProjectId");
                const matchedProject = data.find((p: Project) => p.id === savedProjectId) || data[0];

                if (matchedProject) {
                    setCurrentProject(matchedProject);
                    localStorage.setItem("lastProjectId", matchedProject.id);
                }
            }
        } catch (error) {
            console.error("Failed to fetch projects", error);
        } finally {
            setIsLoading(false);
        }
    };

    const selectProject = (projectId: string) => {
        const project = projects.find(p => p.id === projectId);
        if (project) {
            setCurrentProject(project);
            localStorage.setItem("lastProjectId", project.id);
        }
    };

    const createProject = async (name: string) => {
        const token = localStorage.getItem("authToken");
        if (!token) return;

        try {
            const response = await fetch(`${API_URL}/projects`, {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                    "Authorization": `Bearer ${token}`
                },
                body: JSON.stringify({ name })
            });

            if (response.ok) {
                const newProject = await response.json();
                setProjects(prev => [...prev, newProject]);
                selectProject(newProject.id); // Auto-select new project
            } else {
                const errorData = await response.json().catch(() => ({}));
                throw new Error(errorData.message || "Failed to create project");
            }
        } catch (error) {
            console.error("Create project error", error);
            throw error;
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
