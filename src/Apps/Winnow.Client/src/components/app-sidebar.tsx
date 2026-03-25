import {
  FileText,
  LayoutDashboard,
  Settings,
  Inbox,
  Layers,
  Building2,
  Users,
} from "lucide-react";
import { Link, useNavigate, useLocation } from "react-router-dom";
import {
  Sidebar,
  SidebarContent,
  SidebarFooter,
  SidebarGroup,
  SidebarGroupContent,
  SidebarGroupLabel,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
  SidebarRail,
  SidebarTrigger,
} from "@/components/ui/sidebar";
import { useProject } from "@/context/ProjectContext";
import { ChevronsUpDown, Plus } from "lucide-react";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { useState } from "react";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";

// Menu items.
const items = [
  {
    title: "Cluster Dashboard",
    url: "/dashboard",
    icon: LayoutDashboard,
  },
  {
    title: "All Reports",
    url: "/reports",
    icon: FileText,
  },
  {
    title: "Clusters",
    url: "/clusters",
    icon: Layers,
  },
  {
    title: "Debug Console",
    url: "/debug",
    icon: Inbox,
  },
  {
    title: "Org Settings",
    url: "/settings",
    icon: Settings,
  },
].filter(item => {
  if (item.url === "/debug") return import.meta.env.VITE_DEBUG_MODE === "true";
  return true;
});

export function AppSidebar() {
  const location = useLocation();
  return (
    <Sidebar collapsible="icon" variant="sidebar">
      <SidebarContent>
        {/* Dashboards */}
        <SidebarGroup>
          <SidebarGroupLabel>Dashboards</SidebarGroupLabel>
          <SidebarGroupContent>
            <SidebarMenu>
              <SidebarMenuItem>
                <SidebarMenuButton
                  asChild
                  isActive={location.pathname === "/org-dashboard"}
                >
                  <Link to="/org-dashboard">
                    <Building2 className="h-4 w-4" />
                    <span>Org Overview</span>
                  </Link>
                </SidebarMenuButton>
              </SidebarMenuItem>
              <SidebarMenuItem>
                <SidebarMenuButton
                  asChild
                  isActive={location.pathname === "/team-dashboard"}
                >
                  <Link to="/team-dashboard">
                    <Users className="h-4 w-4" />
                    <span>Team Overview</span>
                  </Link>
                </SidebarMenuButton>
              </SidebarMenuItem>
            </SidebarMenu>
          </SidebarGroupContent>
        </SidebarGroup>

        {/* Project Switcher */}
        <SidebarGroup>
          <SidebarGroupLabel>Project</SidebarGroupLabel>
          <SidebarMenuItem>
            <SidebarMenuButton size="lg" className="w-full justify-between">
              <ProjectSwitcher />
            </SidebarMenuButton>
          </SidebarMenuItem>
          <SidebarGroupContent>
            <SidebarMenu>
              {items.map((item) => (
                <SidebarMenuItem key={item.title}>
                  <SidebarMenuButton asChild>
                    <Link to={item.url}>
                      <item.icon />
                      <span>{item.title}</span>
                    </Link>
                  </SidebarMenuButton>
                </SidebarMenuItem>
              ))}
            </SidebarMenu>
          </SidebarGroupContent>
        </SidebarGroup>
      </SidebarContent>
      <SidebarFooter>
        <div className="flex w-full items-center justify-between p-2 gap-2">
          <SidebarTrigger className="h-4 w-4 text-muted-foreground hover:text-foreground" />
        </div>
      </SidebarFooter>
      <SidebarRail />
    </Sidebar>
  );
}

function ProjectSwitcher() {
  const { projects, currentProject, selectProject, createProject, isLoading } =
    useProject();
  const [isDialogOpen, setIsDialogOpen] = useState(false);
  const [newProjectName, setNewProjectName] = useState("");
  const [isCreating, setIsCreating] = useState(false);
  const navigate = useNavigate();

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!newProjectName.trim()) return;

    setIsCreating(true);
    try {
      const newProject = await createProject(newProjectName);
      setIsDialogOpen(false);
      setNewProjectName("");

      // Navigate to setup page and pass the API key
      if (newProject?.apiKey) {
        navigate("/setup", { state: { apiKey: newProject.apiKey } });
      }
    } catch (error) {
      console.error("Failed to create project", error);
    } finally {
      setIsCreating(false);
    }
  };

  if (isLoading)
    return <div className="animate-pulse h-8 w-24 bg-muted rounded" />;

  return (
    <div className="flex items-center gap-1 w-full">
      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <div className="flex w-full items-center gap-2 cursor-pointer transition-colors hover:bg-muted/50 p-2 rounded-md">
            <div className="flex flex-col items-start text-sm truncate">
              <span className="font-semibold">
                {currentProject?.name || "Select Project"}
              </span>
            </div>
            <ChevronsUpDown className="ml-auto h-4 w-4 opacity-50 shrink-0" />
          </div>
        </DropdownMenuTrigger>
        <DropdownMenuContent
          className="w-[--radix-dropdown-menu-trigger-width] min-w-56 rounded-lg"
          align="start"
          side="bottom"
          sideOffset={4}
        >
          <DropdownMenuLabel className="text-xs text-muted-foreground">
            Projects
          </DropdownMenuLabel>
          {projects.map((project) => (
            <DropdownMenuItem
              key={project.id}
              onClick={() => selectProject(project.id)}
              className="gap-2 p-2"
            >
              <div className="flex h-6 w-6 items-center justify-center rounded-sm border">
                <span className="text-xs font-medium">
                  {project.name.substring(0, 1).toUpperCase()}
                </span>
              </div>
              {project.name}
            </DropdownMenuItem>
          ))}
          <DropdownMenuSeparator />
          <DropdownMenuItem
            className="gap-2 p-2"
            onClick={() => setIsDialogOpen(true)}
          >
            <div className="flex h-6 w-6 items-center justify-center rounded-md border bg-background">
              <Plus className="h-4 w-4" />
            </div>
            <div className="font-medium text-muted-foreground">Add project</div>
          </DropdownMenuItem>
        </DropdownMenuContent>
      </DropdownMenu>

      {currentProject && (
        <Link
          to="/project-settings"
          className="p-2 hover:bg-muted/50 rounded-md transition-colors shrink-0"
          title="Project Settings"
        >
          <Settings className="h-4 w-4 text-muted-foreground hover:text-foreground" />
        </Link>
      )}

      <Dialog open={isDialogOpen} onOpenChange={setIsDialogOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Create Project</DialogTitle>
          </DialogHeader>
          <form onSubmit={handleCreate} className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="projectName">Project Name</Label>
              <Input
                id="projectName"
                placeholder="My Awesome Project"
                value={newProjectName}
                onChange={(e) => setNewProjectName(e.target.value)}
                required
              />
            </div>
            <DialogFooter>
              <Button
                type="button"
                variant="outline"
                onClick={() => setIsDialogOpen(false)}
              >
                Cancel
              </Button>
              <Button type="submit" disabled={isCreating}>
                {isCreating ? "Creating..." : "Create Project"}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>
    </div>
  );
}
