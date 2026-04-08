import React, { useState } from "react";
import { useQueryClient, useQuery } from "@tanstack/react-query";
import { api } from "../lib/api";
import type { Project } from "@/types";
import { ProjectContext } from "@/hooks/use-project";

export function ProjectProvider({ children }: { children: React.ReactNode }) {
  const queryClient = useQueryClient();
  const [selectedProjectId, setSelectedProjectId] = useState<string | null>(null);
  const [orgWide, setOrgWide] = useState(false);

  const { data: projects = [], isLoading } = useQuery<Project[]>({
    queryKey: ["projects", orgWide],
    queryFn: async ({ queryKey }) => {
      const [, isOrgWide] = queryKey;
      const { data } = await api.get("/projects", {
        params: { orgWide: isOrgWide },
      });
      return data;
    },
  });

  const currentProject = React.useMemo(() => {
    if (projects.length === 0) return null;
    const saved = localStorage.getItem("lastProjectId");
    const targetId = selectedProjectId || saved;
    const match = projects.find((p) => p.id === targetId);
    const project = match || projects[0];
    
    // Set storage if it's the first derivation without a saved id
    if (!localStorage.getItem("lastProjectId")) {
      localStorage.setItem("lastProjectId", project.id);
    }
    
    return project;
  }, [projects, selectedProjectId]);

  const refreshProjects = async () => {
    await queryClient.invalidateQueries({ queryKey: ["projects"] });
  };

  const selectProject = (projectId: string) => {
    setSelectedProjectId(projectId);
    localStorage.setItem("lastProjectId", projectId);
    queryClient.invalidateQueries();
  };

  const createProject = async (name: string): Promise<Project> => {
    try {
      const { data: newProject } = await api.post("/projects", { name });
      await refreshProjects();
      selectProject(newProject.id);
      return newProject;
    } catch (error: unknown) {
      console.error("Create project error", error);
      const message = error instanceof Error ? error.message : String(error);
      throw new Error(message);
    }
  };

  const renameProject = async (id: string, newName: string) => {
    try {
      await api.put(`/projects/${id}`, { name: newName });
      await refreshProjects();
    } catch (error: unknown) {
      console.error("Rename project error", error);
      const message = error instanceof Error ? error.message : String(error);
      throw new Error(message);
    }
  };

  const updateProjectSettings = async (id: string, updateData: Partial<Project>) => {
    try {
      // Maps to UpdateProjectCommand structure
      const payload = {
        name: updateData.name,
        teamId: updateData.teamId,
        notificationThreshold: updateData.notifications?.volumeThreshold,
        criticalityThreshold: updateData.notifications?.criticalityThreshold,
      };
      
      await api.put(`/projects/${id}`, payload);
      await refreshProjects();
    } catch (error: unknown) {
      console.error("Update project error", error);
      const message = error instanceof Error ? error.message : String(error);
      throw new Error(message);
    }
  };

  const deleteProject = async (id: string) => {
    try {
      await api.delete(`/projects/${id}`);
      await refreshProjects();

      if (currentProject?.id === id) {
        const nextProject = projects.find((p: Project) => p.id !== id);
        if (nextProject) {
          selectProject(nextProject.id);
        } else {
          setSelectedProjectId(null);
          localStorage.removeItem("lastProjectId");
          queryClient.invalidateQueries();
        }
      }
    } catch (error: unknown) {
      console.error("Delete project error", error);
      const message = error instanceof Error ? error.message : String(error);
      throw new Error(message);
    }
  };

  return (
    <ProjectContext.Provider
      value={{
        projects,
        currentProject,
        isLoading,
        orgWide,
        setOrgWide,
        selectProject,
        refreshProjects,
        createProject,
        renameProject,
        updateProjectSettings,
        deleteProject,
      }}
    >
      {children}
    </ProjectContext.Provider>
  );
}
