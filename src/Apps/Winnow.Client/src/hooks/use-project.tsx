import { createContext, useContext } from "react";
import type { Project } from "@/types";

export interface ProjectContextType {
  projects: Project[];
  currentProject: Project | null;
  isLoading: boolean;
  orgWide: boolean;
  setOrgWide: (value: boolean) => void;
  selectProject: (projectId: string) => void;
  refreshProjects: () => Promise<void>;
  createProject: (name: string) => Promise<Project>;
  renameProject: (id: string, newName: string) => Promise<void>;
  updateProjectSettings: (id: string, data: Partial<Project>) => Promise<void>;
  deleteProject: (id: string) => Promise<void>;
}

export const ProjectContext = createContext<ProjectContextType | undefined>(undefined);

export function useProject() {
  const context = useContext(ProjectContext);
  if (context === undefined) {
    throw new Error("useProject must be used within a ProjectProvider");
  }
  return context;
}
