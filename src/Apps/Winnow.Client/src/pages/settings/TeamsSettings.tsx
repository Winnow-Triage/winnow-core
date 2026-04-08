import { useState, useEffect } from "react";
import { useQuery } from "@tanstack/react-query";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetHeader,
  SheetTitle,
} from "@/components/ui/sheet";
import { ScrollArea } from "@/components/ui/scroll-area";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { Avatar, AvatarFallback } from "@/components/ui/avatar";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { Separator } from "@/components/ui/separator";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogTrigger,
} from "@/components/ui/alert-dialog";
import {
  Loader2,
  AlertCircle,
  Settings2,
  Users,
  UserPlus,
  MoreHorizontal,
} from "lucide-react";
import { toast } from "sonner";
import { api } from "@/lib/api";
import { useProject } from "@/hooks/use-project";
import type { Project, TeamDetail, OrgMember } from "@/types";

interface TeamsSettingsProps {
  organizationId?: string;
}

export function TeamsSettings({ organizationId }: TeamsSettingsProps) {
  const { projects, refreshProjects, setOrgWide } = useProject();
  const [selectedTeamId, setSelectedTeamId] = useState<string | null>(null);
  const [memberSelects, setMemberSelects] = useState<Record<string, string>>(
    {},
  );

  useEffect(() => {
    setOrgWide(true);
    return () => setOrgWide(false);
  }, [setOrgWide]);

  const {
    data: teams,
    isLoading,
    error,
    refetch,
  } = useQuery<TeamDetail[]>({
    queryKey: ["teams", organizationId],
    queryFn: async () => {
      const { data } = await api.get("/teams");
      return data;
    },
    enabled: !!organizationId,
    retry: 0,
  });

  const { data: orgMembers } = useQuery<OrgMember[]>({
    queryKey: ["org-members", organizationId],
    queryFn: async () => {
      const { data } = await api.get("/organizations/current/members");
      return data;
    },
    enabled: !!organizationId,
    retry: 0,
  });

  const [newTeamName, setNewTeamName] = useState("");
  const [isCreating, setIsCreating] = useState(false);

  const handleCreateTeam = async () => {
    if (!newTeamName.trim()) return;
    setIsCreating(true);
    try {
      await api.post("/teams", { name: newTeamName.trim() });
      setNewTeamName("");
      await refetch();
      toast.success("Team created successfully");
    } catch {
      toast.error("Failed to create team");
    } finally {
      setIsCreating(false);
    }
  };

  const handleDeleteTeam = async (id: string) => {
    try {
      await api.delete(`/teams/${id}`);
      await refetch();
      await refreshProjects();
      toast.success("Team deleted successfully");
    } catch {
      toast.error("Failed to delete team");
    }
  };

  const handleAssignProject = async (
    projectId: string,
    teamId: string | null,
  ) => {
    const project = projects.find((p: { id: string }) => p.id === projectId);
    if (!project) return;

    try {
      await api.put(`/projects/${projectId}`, {
        name: project.name,
        teamId: teamId === "none" ? null : teamId,
      });
      await refreshProjects();
      await refetch();
      toast.success(
        teamId && teamId !== "none" ? "Project assigned to team" : "Project unassigned from team",
      );
    } catch {
      toast.error("Failed to update project assignment");
    }
  };

  const handleAddTeamMember = async (teamId: string, userId: string) => {
    if (!userId) return;
    setMemberSelects((prev) => ({ ...prev, [teamId]: userId }));
    try {
      await api.post(`/teams/${teamId}/members`, { userId });
      await refetch();
      toast.success("Member added to team");
      setMemberSelects((prev) => ({ ...prev, [teamId]: "" }));
    } catch {
      toast.error("Failed to add member to team");
    }
  };

  const handleRemoveTeamMember = async (teamId: string, userId: string) => {
    try {
      await api.delete(`/teams/${teamId}/members/${userId}`);
      await refetch();
      toast.success("Member removed from team");
    } catch {
      toast.error("Failed to remove member from team");
    }
  };

  if (isLoading) {
    return (
      <div className="py-20 text-center text-muted-foreground">
        <Loader2 className="h-8 w-8 animate-spin mx-auto mb-2 opacity-50" />
        Loading teams...
      </div>
    );
  }

  if (error) {
    return (
      <div className="flex flex-col items-center justify-center py-20 text-center border rounded-lg bg-muted/20 border-dashed">
        <AlertCircle className="h-8 w-8 text-destructive mb-2" />
        <h4 className="font-semibold">Access Denied</h4>
        <p className="text-sm text-muted-foreground">
          {(error as { response?: { data?: { detail?: string } } }).response?.data?.detail || "You don't have permission to manage teams."}
        </p>
      </div>
    );
  }

  const selectedTeam = teams?.find((t) => t.id === selectedTeamId);

  const formatCount = (count: number, label: string) => {
    return `${count} ${label}${count === 1 ? "" : "s"}`;
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle>Teams</CardTitle>
        <CardDescription>
          Manage teams within your organization. Projects can be assigned to
          teams for better organization.
        </CardDescription>
      </CardHeader>
      <CardContent className="space-y-6">
        <div className="flex gap-2">
          <Input
            placeholder="New team name..."
            value={newTeamName}
            onChange={(e) => setNewTeamName(e.target.value)}
            className="max-w-sm"
          />
          <Button
            onClick={handleCreateTeam}
            disabled={isCreating || !newTeamName.trim()}
          >
            {isCreating ? "Creating..." : "Add Team"}
          </Button>
        </div>

        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {teams?.length === 0 ? (
            <div className="p-8 text-center text-muted-foreground text-sm border rounded-md col-span-full">
              No teams found. Create one to get started.
            </div>
          ) : (
            teams?.map((team) => (
              <button
                key={team.id}
                onClick={() => setSelectedTeamId(team.id)}
                className="group flex flex-col items-start p-4 text-left border rounded-lg hover:border-primary/50 hover:bg-muted/30 transition-all"
              >
                <div className="flex items-center justify-between w-full mb-1">
                  <h3 className="font-semibold group-hover:text-primary transition-colors">
                    {team.name}
                  </h3>
                  <Settings2 className="h-4 w-4 text-muted-foreground opacity-0 group-hover:opacity-100 transition-opacity" />
                </div>
                <div className="flex items-center gap-3 text-xs text-muted-foreground">
                  <span className="flex items-center gap-1">
                    <Users className="h-3 w-3" />
                    {formatCount(team.members?.length || 0, "member")}
                  </span>
                  <span>•</span>
                  <span>{formatCount(team.projectCount, "project")}</span>
                </div>
              </button>
            ))
          )}
        </div>

        <div className="pt-6 border-t font-semibold">
          <h3>Unassigned Projects</h3>
        </div>

        <div className="grid gap-3">
          {projects.filter((p: Project) => !p.teamId).length === 0 ? (
            <div className="p-4 text-center text-muted-foreground text-sm border rounded-md">
              All projects are assigned to teams.
            </div>
          ) : (
            projects
              .filter((p: Project) => !p.teamId)
              .map((project: Project) => (
                <div
                  key={project.id}
                  className="flex items-center justify-between p-3 border rounded-md"
                >
                  <span className="text-sm font-medium">{project.name}</span>
                  <Select
                    onValueChange={(value: string) =>
                      handleAssignProject(project.id, value)
                    }
                  >
                    <SelectTrigger className="h-8 w-[180px] text-xs">
                      <SelectValue placeholder="Assign to team..." />
                    </SelectTrigger>
                    <SelectContent>
                      {teams?.map((team: { id: string; name: string }) => (
                        <SelectItem key={team.id} value={team.id}>
                          {team.name}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
              ))
          )}
        </div>

        <TeamDetailsDrawer
          team={selectedTeam}
          onClose={() => setSelectedTeamId(null)}
          orgMembers={orgMembers || []}
          projects={selectedTeam?.projects || []}
          onAddMember={(userId) => handleAddTeamMember(selectedTeamId!, userId)}
          onRemoveMember={(userId) =>
            handleRemoveTeamMember(selectedTeamId!, userId)
          }
          onDeleteTeam={() => {
            handleDeleteTeam(selectedTeamId!);
            setSelectedTeamId(null);
          }}
          onUnassignProject={(projectId) =>
            handleAssignProject(projectId, "none")
          }
          memberSelectValue={
            selectedTeamId ? memberSelects[selectedTeamId] || "" : ""
          }
        />
      </CardContent>
    </Card>
  );
}

function TeamDetailsDrawer({
  team,
  onClose,
  orgMembers,
  projects,
  onAddMember,
  onRemoveMember,
  onDeleteTeam,
  onUnassignProject,
  memberSelectValue,
}: {
  team: TeamDetail | undefined;
  onClose: () => void;
  orgMembers: OrgMember[];
  projects: { id: string; name: string }[];
  onAddMember: (userId: string) => void;
  onRemoveMember: (userId: string) => void;
  onDeleteTeam: () => void;
  onUnassignProject: (projectId: string) => void;
  memberSelectValue: string;
}) {
  return (
    <Sheet open={!!team} onOpenChange={(open) => !open && onClose()}>
      <SheetContent className="sm:max-w-[500px] w-full p-0 flex flex-col">
        <SheetHeader className="p-6 pb-2">
          <SheetTitle>{team?.name}</SheetTitle>
          <SheetDescription>
            Manage team members and assigned projects.
          </SheetDescription>
        </SheetHeader>

        <ScrollArea className="flex-1 px-6">
          <div className="space-y-8 py-4">
            {/* Members Section */}
            <div className="space-y-4">
              <div className="flex items-center justify-between">
                <h4 className="text-sm font-semibold uppercase tracking-wider text-muted-foreground flex items-center gap-2">
                  <Users className="h-4 w-4" />
                  Members
                </h4>
                <Select value={memberSelectValue} onValueChange={onAddMember}>
                  <SelectTrigger className="h-8 w-[160px] text-xs">
                    <UserPlus className="h-3 w-3 mr-1" />
                    <span>Add Member...</span>
                  </SelectTrigger>
                  <SelectContent>
                    {orgMembers
                      .filter(
                        (om) =>
                          om.status === "Active" &&
                          !(team?.members || []).some(
                            (tm) => tm.userId === om.id,
                          ),
                      )
                      .map((member) => (
                        <SelectItem key={member.id} value={member.id}>
                          {member.fullName || member.email}
                        </SelectItem>
                      ))}
                    {orgMembers.filter(
                      (om) =>
                        om.status === "Active" &&
                        !(team?.members || []).some(
                          (tm) => tm.userId === om.id,
                        ),
                    ).length === 0 && (
                        <div className="p-2 text-xs text-muted-foreground text-center">
                          All active members already in team
                        </div>
                      )}
                  </SelectContent>
                </Select>
              </div>

              <div className="border rounded-md">
                <Table>
                  <TableHeader>
                    <TableRow className="hover:bg-transparent bg-muted/50">
                      <TableHead className="w-[200px]">User</TableHead>
                      <TableHead>Role</TableHead>
                      <TableHead className="text-right">Actions</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {(team?.members || []).length === 0 ? (
                      <TableRow>
                        <TableCell
                          colSpan={3}
                          className="h-24 text-center text-muted-foreground italic"
                        >
                          No members in this team.
                        </TableCell>
                      </TableRow>
                    ) : (
                      team?.members.map((member) => (
                        <TableRow key={member.userId} className="group">
                          <TableCell>
                            <div className="flex items-center gap-3">
                              <Avatar className="h-8 w-8">
                                <AvatarFallback className="text-[10px] bg-primary/10 text-primary">
                                  {(member.fullName || "U")
                                    .split(" ")
                                    .map((n) => n[0])
                                    .join("")
                                    .toUpperCase()}
                                </AvatarFallback>
                              </Avatar>
                              <div className="flex flex-col">
                                <span className="font-medium text-sm">
                                  {member.fullName}
                                </span>
                              </div>
                            </div>
                          </TableCell>
                          <TableCell className="text-xs text-muted-foreground">
                            Member
                          </TableCell>
                          <TableCell className="text-right">
                            <DropdownMenu>
                              <DropdownMenuTrigger asChild>
                                <Button
                                  variant="ghost"
                                  size="sm"
                                  className="h-8 w-8 p-0"
                                >
                                  <MoreHorizontal className="h-4 w-4" />
                                </Button>
                              </DropdownMenuTrigger>
                              <DropdownMenuContent align="end">
                                <DropdownMenuItem
                                  onClick={() => onRemoveMember(member.userId)}
                                  className="text-destructive focus:text-destructive"
                                >
                                  Remove from team
                                </DropdownMenuItem>
                              </DropdownMenuContent>
                            </DropdownMenu>
                          </TableCell>
                        </TableRow>
                      ))
                    )}
                  </TableBody>
                </Table>
              </div>
            </div>

            <Separator />

            {/* Projects Section */}
            <div className="space-y-4">
              <h4 className="text-sm font-semibold uppercase tracking-wider text-muted-foreground">
                Assigned Projects
              </h4>
              <div className="grid gap-2">
                {projects.length === 0 ? (
                  <div className="border border-dashed rounded-lg p-8 text-center bg-muted/20">
                    <p className="text-sm text-muted-foreground">
                      No projects assigned to this team.
                    </p>
                  </div>
                ) : (
                  projects.map((project) => (
                    <div
                      key={project.id}
                      className="flex items-center justify-between p-3 border rounded-lg bg-muted/5"
                    >
                      <span className="text-sm font-medium">
                        {project.name}
                      </span>
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => onUnassignProject(project.id)}
                        className="text-xs h-7 text-muted-foreground hover:text-foreground"
                      >
                        Unassign
                      </Button>
                    </div>
                  ))
                )}
              </div>
            </div>

            {/* Danger Zone */}
            <div className="pt-8 opacity-50 hover:opacity-100 transition-opacity">
              <div className="p-4 border border-destructive/20 rounded-lg bg-destructive/5 space-y-3">
                <div>
                  <h4 className="text-sm font-bold text-destructive uppercase tracking-tight">
                    Danger Zone
                  </h4>
                  <p className="text-xs text-muted-foreground">
                    This action will permanently remove the team. Projects will
                    be unassigned but not deleted.
                  </p>
                </div>

                <AlertDialog>
                  <AlertDialogTrigger asChild>
                    <Button variant="destructive" size="sm" className="w-full">
                      Delete Team
                    </Button>
                  </AlertDialogTrigger>
                  <AlertDialogContent>
                    <AlertDialogHeader>
                      <AlertDialogTitle>
                        Are you absolutely sure?
                      </AlertDialogTitle>
                      <AlertDialogDescription>
                        This will permanently delete the{" "}
                        <span className="font-bold text-foreground">
                          {team?.name}
                        </span>{" "}
                        team. Assigned projects will be unassigned but not
                        deleted. This action cannot be undone.
                      </AlertDialogDescription>
                    </AlertDialogHeader>
                    <AlertDialogFooter>
                      <AlertDialogCancel>Cancel</AlertDialogCancel>
                      <AlertDialogAction
                        onClick={onDeleteTeam}
                        className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
                      >
                        Yes, delete team
                      </AlertDialogAction>
                    </AlertDialogFooter>
                  </AlertDialogContent>
                </AlertDialog>
              </div>
            </div>
          </div>
        </ScrollArea>
      </SheetContent>
    </Sheet>
  );
}
