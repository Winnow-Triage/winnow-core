import { useState, useEffect } from "react";
import { useQuery } from "@tanstack/react-query";
import {
  api,
  resendInvitation,
  cancelInvitation,
  removeOrganizationMember,
  toggleMemberLock,
  updateMemberRole,
} from "@/lib/api";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { toast } from "sonner";
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
import { UserPlus, MoreHorizontal, Loader2, AlertCircle } from "lucide-react";
import type { OrgMember } from "@/types";

interface Role {
  id: string;
  name: string;
}

interface MembersSettingsProps {
  organizationId?: string;
}

export function MembersSettings({ organizationId }: MembersSettingsProps) {
  const { data: teams, error: teamsError } = useQuery<
    { id: string; name: string; members: { userId: string }[] }[]
  >({
    queryKey: ["teams", organizationId],
    queryFn: async () => {
      const { data } = await api.get("/teams");
      return data;
    },
    enabled: !!organizationId,
    retry: 0,
  });

  const {
    data: members,
    isLoading: isMembersLoading,
    error: membersError,
    refetch: refetchMembers,
  } = useQuery<OrgMember[]>({
    queryKey: ["org-members", organizationId],
    queryFn: async () => {
      const { data } = await api.get("/organizations/current/members");
      return data;
    },
    enabled: !!organizationId,
    retry: 0,
  });

  const { data: roles, isLoading: isRolesLoading, error: rolesError } = useQuery<Role[]>({
    queryKey: ["org-roles", organizationId],
    queryFn: async () => {
      const { data } = await api.get(`/organizations/${organizationId}/roles`);
      return data.roles;
    },
    enabled: !!organizationId,
    retry: 0,
  });

  const handleUpdateRole = async (userId: string, targetRoleId: string) => {
    if (!organizationId) return;
    try {
      await updateMemberRole(organizationId, userId, targetRoleId);
      toast.success("Role updated");
      await refetchMembers();
    } catch (error) {
      const err = error as { response?: { data?: { errors?: { GeneralErrors?: string[]; generalErrors?: string[] }; message?: string } } };
      toast.error(
        err.response?.data?.errors?.GeneralErrors?.[0] ||
        err.response?.data?.errors?.generalErrors?.[0] ||
        err.response?.data?.message || 
        "Failed to update role"
      );
    }
  };

  const [isInviteModalOpen, setIsInviteModalOpen] = useState(false);

  const handleResendInvitation = async (
    invitationId: string,
    email: string,
  ) => {
    if (!organizationId) return;
    try {
      await resendInvitation(organizationId, invitationId);
      toast.success(`Invitation resent to ${email}`);
      await refetchMembers();
    } catch {
      toast.error("Failed to resend invitation");
    }
  };

  const handleCancelInvitation = async (invitationId: string) => {
    if (!organizationId) return;
    try {
      await cancelInvitation(organizationId, invitationId);
      toast.success("Invitation cancelled");
      await refetchMembers();
    } catch {
      toast.error("Failed to cancel invitation");
    }
  };

  const handleRemoveMember = async (userId: string) => {
    if (!organizationId) return;
    try {
      await removeOrganizationMember(organizationId, userId);
      toast.success("Member removed from organization");
      await refetchMembers();
    } catch {
      toast.error("Failed to remove member");
    }
  };

  const handleToggleMemberLock = async (userId: string) => {
    if (!organizationId) return;
    try {
      const data = await toggleMemberLock(organizationId, userId);
      toast.success(
        data.isLocked ? "Member access locked" : "Member access restored",
      );
      await refetchMembers();
    } catch {
      toast.error("Failed to update member lock status");
    }
  };

  if (isMembersLoading || isRolesLoading) {
    return (
      <div className="py-20 text-center text-muted-foreground">
        <Loader2 className="h-8 w-8 animate-spin mx-auto mb-2 opacity-50" />
        Loading members...
      </div>
    );
  }

  if (membersError || rolesError || teamsError) {
    const error = membersError || rolesError || teamsError;
    return (
      <div className="flex flex-col items-center justify-center py-20 text-center border rounded-lg bg-muted/20 border-dashed">
        <AlertCircle className="h-8 w-8 text-destructive mb-2" />
        <h4 className="font-semibold">Access Denied</h4>
        <p className="text-sm text-muted-foreground">
          {(error as { response?: { data?: { detail?: string } } }).response?.data?.detail || "You don't have permission to manage members or roles."}
        </p>
      </div>
    );
  }

  // Helper to get teams for a user
  const getUserTeams = (userId: string) => {
    if (!teams) return [];
    return teams.filter((t) => t.members.some((m) => m.userId === userId));
  };

  const formatTeamList = (teams: { name: string }[]) => {
    if (teams.length === 0) return "No teams";
    if (teams.length === 1) return teams[0].name;
    return `${teams[0].name}, +${teams.length - 1} more`;
  };

  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between space-y-0">
        <div>
          <CardTitle>Organization Directory</CardTitle>
          <CardDescription>
            Manage your organization's members and their global roles.
          </CardDescription>
        </div>
        <Button onClick={() => setIsInviteModalOpen(true)}>
          <UserPlus className="h-4 w-4 mr-2" />
          Invite Member
        </Button>
      </CardHeader>
      <CardContent>
        <div className="border rounded-md">
          <Table>
            <TableHeader>
              <TableRow className="bg-muted/50 hover:bg-muted/50">
                <TableHead>User</TableHead>
                <TableHead>Global Role</TableHead>
                <TableHead>Teams</TableHead>
                <TableHead>Status</TableHead>
                <TableHead className="text-right">Actions</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {members?.map((member) => {
                const userTeams = getUserTeams(member.id);
                const isPending = member.status === "Pending";

                return (
                  <TableRow key={member.id}>
                    <TableCell>
                      <div className="flex items-center gap-3">
                        <Avatar className="h-9 w-9">
                          <AvatarFallback className="bg-primary/10 text-primary text-xs">
                            {member.fullName
                              ? member.fullName
                                .split(" ")
                                .map((n) => n[0])
                                .join("")
                                .toUpperCase()
                              : "?"}
                          </AvatarFallback>
                        </Avatar>
                        <div className="flex flex-col">
                          <span className="font-semibold text-sm">
                            {member.fullName || member.email}
                          </span>
                          <span className="text-xs text-muted-foreground">
                            {member.email}
                          </span>
                        </div>
                      </div>
                    </TableCell>
                    <TableCell>
                      {member.status === "Active" ? (
                        <Select
                          value={member.roleId}
                          onValueChange={(val) => handleUpdateRole(member.id, val)}
                        >
                          <SelectTrigger className="h-8 max-w-[150px]">
                            <SelectValue placeholder="Select a role" />
                          </SelectTrigger>
                          <SelectContent>
                            {roles?.map((role) => (
                              <SelectItem key={role.id} value={role.id}>
                                {role.name}
                              </SelectItem>
                            ))}
                          </SelectContent>
                        </Select>
                      ) : (
                        <span className="text-sm">
                          {member.globalRole === "owner"
                            ? "Org Owner"
                            : member.globalRole}
                        </span>
                      )}
                    </TableCell>
                    <TableCell>
                      <div className="flex flex-col">
                        <span className="text-sm">
                          {formatTeamList(userTeams)}
                        </span>
                        {userTeams.length > 0 && (
                          <span className="text-[10px] text-muted-foreground">
                            {userTeams.length}{" "}
                            {userTeams.length === 1 ? "team" : "teams"}
                          </span>
                        )}
                      </div>
                    </TableCell>
                    <TableCell>
                      {isPending ? (
                        <div className="inline-flex items-center gap-1.5 px-2 py-0.5 rounded-full text-amber-600 bg-amber-50 border border-amber-100">
                          <div className="h-1.5 w-1.5 rounded-full bg-amber-500" />
                          <span className="text-[10px] font-semibold uppercase tracking-wider">
                            Pending Invite
                          </span>
                        </div>
                      ) : (
                        <div className="flex items-center gap-1.5">
                          <div
                            className={`h-1.5 w-1.5 rounded-full ${member.isLocked ? "bg-zinc-400" : "bg-green-500"}`}
                          />
                          <span className="text-xs font-medium">
                            {member.isLocked ? "Locked" : "Active"}
                          </span>
                        </div>
                      )}
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
                          {isPending ? (
                            <>
                              <DropdownMenuItem
                                onClick={() =>
                                  handleResendInvitation(
                                    member.id,
                                    member.email,
                                  )
                                }
                              >
                                Resend Invitation
                              </DropdownMenuItem>
                              <DropdownMenuItem
                                onClick={() =>
                                  handleCancelInvitation(member.id)
                                }
                                className="text-destructive focus:text-destructive"
                              >
                                Cancel Invitation
                              </DropdownMenuItem>
                            </>
                          ) : (
                            <>
                              <DropdownMenuItem
                                onClick={() =>
                                  handleToggleMemberLock(member.id)
                                }
                              >
                                {member.isLocked
                                  ? "Restore Access"
                                  : "Lock Access"}
                              </DropdownMenuItem>
                              <DropdownMenuItem
                                onClick={() => handleRemoveMember(member.id)}
                                className="text-destructive focus:text-destructive"
                              >
                                Remove from Organization
                              </DropdownMenuItem>
                            </>
                          )}
                        </DropdownMenuContent>
                      </DropdownMenu>
                    </TableCell>
                  </TableRow>
                );
              })}
            </TableBody>
          </Table>
        </div>

        <InviteMemberModal
          isOpen={isInviteModalOpen}
          onClose={() => setIsInviteModalOpen(false)}
          organizationId={organizationId}
          roles={roles || []}
        />
      </CardContent>
    </Card>
  );
}

function InviteMemberModal({
  isOpen,
  onClose,
  organizationId,
  roles,
}: {
  isOpen: boolean;
  onClose: () => void;
  organizationId?: string;
  roles: Role[];
}) {
  const [email, setEmail] = useState("");
  const [roleId, setRoleId] = useState<string>("");

  useEffect(() => {
    if (isOpen && roles.length > 0 && !roleId) {
      const memberRole = roles.find(r => r.name === "Member") || roles[0];
      if (memberRole) setRoleId(memberRole.id);
    }
  }, [isOpen, roles, roleId]);
  const [selectedTeams, setSelectedTeams] = useState<string[]>([]);
  const [isInviting, setIsInviting] = useState(false);
  const [searchTerm, setSearchTerm] = useState("");

  const { data: teams } = useQuery<{ id: string; name: string }[]>({
    queryKey: ["teams", organizationId],
    queryFn: async () => {
      const { data } = await api.get("/teams");
      return data;
    },
    enabled: isOpen && !!organizationId,
    retry: 0,
  });

  const filteredTeams =
    teams?.filter((team) =>
      team.name.toLowerCase().includes(searchTerm.toLowerCase()),
    ) || [];

  const handleInvite = async () => {
    if (!email.trim() || !organizationId) return;
    setIsInviting(true);
    try {
      await api.post(`/organizations/${organizationId}/invitations`, {
        orgId: organizationId,
        email: email.trim(),
        roleId: roleId,
        teamIds: selectedTeams,
      });
      toast.success(`Invite sent to ${email}`);
      onClose();
      // Reset state
      setEmail("");
      setSelectedTeams([]);
      setSearchTerm("");
    } catch (error) {
      console.error("Failed to send invite:", error);
      toast.error("Failed to send invitation. Please try again.");
    } finally {
      setIsInviting(false);
    }
  };

  return (
    <Dialog open={isOpen} onOpenChange={onClose}>
      <DialogContent className="sm:max-w-[425px] p-0 flex flex-col overflow-hidden border-border bg-background">
        <DialogHeader className="p-6 pb-0">
          <DialogTitle>Invite Member</DialogTitle>
          <DialogDescription className="text-muted-foreground">
            Send an invitation to join your organization and assign an initial
            role/teams.
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-6 p-6 overflow-y-auto">
          <div className="space-y-2">
            <Label className="text-sm font-semibold text-muted-foreground">
              Email Address
            </Label>
            <Input
              placeholder="colleague@example.com"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              className="bg-muted/50 border-border focus:ring-primary"
            />
          </div>

          <div className="space-y-2">
            <Label className="text-sm font-semibold text-muted-foreground">
              Initial Role
            </Label>
            <Select value={roleId} onValueChange={setRoleId}>
              <SelectTrigger className="bg-muted/50 border-border">
                <SelectValue placeholder="Select a role" />
              </SelectTrigger>
              <SelectContent className="bg-popover border-border">
                {roles.map(r => (
                  <SelectItem key={r.id} value={r.id}>{r.name}</SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          <div className="space-y-3">
            <div className="flex items-center justify-between">
              <Label className="text-sm font-semibold text-muted-foreground">
                Assign to Teams
              </Label>
              {teams && teams.length > 5 && (
                <span className="text-[10px] text-muted-foreground bg-muted px-1.5 py-0.5 rounded">
                  {teams.length} total
                </span>
              )}
            </div>

            <div className="space-y-2">
              <Input
                placeholder="Search teams..."
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                className="h-8 text-xs bg-muted/30 border-border"
              />

              <div className="max-h-40 overflow-y-auto border border-border rounded-md p-1 bg-muted/20">
                <div className="flex flex-col">
                  {filteredTeams.length === 0 && (
                    <div className="p-4 text-xs text-muted-foreground text-center">
                      {searchTerm ? "No matching teams" : "No teams found"}
                    </div>
                  )}
                  {filteredTeams.map((team) => (
                    <label
                      key={team.id}
                      className="flex items-center gap-3 p-2 hover:bg-accent rounded cursor-pointer transition-colors"
                    >
                      <input
                        type="checkbox"
                        checked={selectedTeams.includes(team.id)}
                        onChange={(e) => {
                          if (e.target.checked)
                            setSelectedTeams([...selectedTeams, team.id]);
                          else
                            setSelectedTeams(
                              selectedTeams.filter((id) => id !== team.id),
                            );
                        }}
                        className="h-4 w-4 rounded border-input bg-background accent-primary cursor-pointer"
                      />
                      <span className="text-sm text-foreground truncate">
                        {team.name}
                      </span>
                    </label>
                  ))}
                </div>
              </div>
            </div>
          </div>
        </div>

        <DialogFooter className="p-6 bg-muted/30 border-t border-border">
          <Button
            variant="outline"
            onClick={onClose}
            disabled={isInviting}
            className="border-border hover:bg-accent"
          >
            Cancel
          </Button>
          <Button onClick={handleInvite} disabled={isInviting || !email.trim()}>
            {isInviting ? "Sending..." : "Send Invitation"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
