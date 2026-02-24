import React, { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { api } from '@/lib/api';
import { Card, CardContent, CardDescription, CardHeader, CardTitle, CardFooter } from "@/components/ui/card";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle, DialogTrigger } from "@/components/ui/dialog";
import { toast } from "sonner";
import { useNavigate, useSearchParams } from "react-router-dom";
import { useProject } from '@/context/ProjectContext';
import type { Project } from '@/context/ProjectContext';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Users, UserPlus, MoreHorizontal, Settings2 } from 'lucide-react';
import { Sheet, SheetContent, SheetDescription, SheetHeader, SheetTitle } from "@/components/ui/sheet";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Avatar, AvatarFallback } from "@/components/ui/avatar";
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from "@/components/ui/dropdown-menu";
import { Separator } from "@/components/ui/separator";
import { ScrollArea } from "@/components/ui/scroll-area";
import {
    AlertDialog,
    AlertDialogAction,
    AlertDialogCancel,
    AlertDialogContent,
    AlertDialogDescription,
    AlertDialogFooter,
    AlertDialogHeader,
    AlertDialogTitle,
    AlertDialogTrigger
} from "@/components/ui/alert-dialog";

export default function Settings() {
    const [searchParams, setSearchParams] = useSearchParams();
    const currentTab = searchParams.get('tab') || 'general';

    const handleTabChange = (value: string) => {
        setSearchParams({ tab: value });
    };

    const [isCheckingOut, setIsCheckingOut] = useState<string | null>(null);
    const [isManaging, setIsManaging] = useState(false);

    const { data: organization, isLoading: isOrgLoading, refetch } = useQuery<{ id: string, name: string, subscriptionTier: string }>({
        queryKey: ['current-organization'],
        queryFn: async () => {
            const { data } = await api.get('/organizations/current');
            return data;
        }
    });

    const [orgName, setOrgName] = useState("");
    const [isSavingOrg, setIsSavingOrg] = useState(false);
    const [isDeletingOrg, setIsDeletingOrg] = useState(false);
    const [isDeleteConfirmOpen, setIsDeleteConfirmOpen] = useState(false);
    const navigate = useNavigate();

    // Sync input with fetched org name
    React.useEffect(() => {
        if (organization) {
            setOrgName(organization.name);
        }
    }, [organization]);

    const handleSaveOrganization = async () => {
        if (!orgName.trim() || orgName.trim() === organization?.name) return;

        setIsSavingOrg(true);
        try {
            await api.put('/organizations/current', { name: orgName.trim() });
            await refetch();
            toast.success("Organization updated successfully");
        } catch (error) {
            console.error("Failed to update organization:", error);
            toast.error("Failed to update organization");
        } finally {
            setIsSavingOrg(false);
        }
    };

    const handleDeleteOrganization = async () => {
        setIsDeletingOrg(true);
        try {
            await api.delete('/organizations/current');
            // Remove token and push to login since org no longer exists
            localStorage.removeItem('authToken');
            toast.success("Organization deleted. You have been logged out.");
            navigate('/login');
        } catch (error) {
            console.error("Failed to delete organization:", error);
            toast.error("Failed to delete organization. Please contact support.");
        } finally {
            setIsDeletingOrg(false);
            setIsDeleteConfirmOpen(false);
        }
    };

    const subscriptionTier: string = organization?.subscriptionTier || "Free";

    const getButtonText = (targetTier: string, currentTier: string, checkingOut: string | null) => {
        if (checkingOut === targetTier) return "Redirecting...";
        if (currentTier === targetTier) return "Current Plan";

        const tiers = ["Free", "Starter", "Pro", "Enterprise"];
        const currentIndex = tiers.indexOf(currentTier);
        const targetIndex = tiers.indexOf(targetTier);

        if (currentIndex !== -1 && targetIndex !== -1 && targetIndex < currentIndex) {
            return `Downgrade to ${targetTier}`;
        }

        if (targetTier === "Enterprise") return "Contact Sales / Upgrade";
        return `Upgrade to ${targetTier}`;
    };

    const handleCheckout = async (tier: string) => {
        setIsCheckingOut(tier);

        // If the action is a downgrade or an upgrade of an existing paid plan, route to the Customer Portal instead
        // This prevents double billing by allowing Stripe to handle prorations/cancellations of the current active plan.
        const actionText = getButtonText(tier, subscriptionTier, null);
        if (actionText.includes("Downgrade") || (subscriptionTier !== "Free" && actionText.includes("Upgrade"))) {
            await handleManageSubscription(tier === "Free" ? "cancel" : "update");
            setIsCheckingOut(null);
            return;
        }

        try {
            const { data } = await api.post('/billing/checkout', { targetTier: tier });
            if (data?.checkoutUrl) {
                window.location.href = data.checkoutUrl;
            }
        } catch (error) {
            console.error("Checkout failed:", error);
            toast.error("Failed to start checkout process. Please try again.");
        } finally {
            setIsCheckingOut(null);
        }
    };

    const handleManageSubscription = async (action?: string) => {
        setIsManaging(true);
        try {
            const { data } = await api.post('/billing/portal', { action: action ?? null });
            if (data?.portalUrl) {
                window.location.href = data.portalUrl;
            }
        } catch (error) {
            console.error("Portal redirect failed:", error);
            toast.error("Failed to open billing portal. Please try again.");
        } finally {
            setIsManaging(false);
        }
    };

    return (
        <div className="max-w-4xl w-full mx-auto py-8">
            <div className="mb-8">
                <h1 className="text-3xl font-bold tracking-tight">Organization Settings</h1>
                <p className="text-muted-foreground">Manage settings and access for {organization?.name || "your organization"}</p>
            </div>

            <Tabs value={currentTab} onValueChange={handleTabChange} className="w-full">
                <TabsList className="grid w-full grid-cols-5 max-w-[700px]">
                    <TabsTrigger value="general">General</TabsTrigger>
                    <TabsTrigger value="members">Members</TabsTrigger>
                    <TabsTrigger value="teams">Teams</TabsTrigger>
                    <TabsTrigger value="billing">Billing</TabsTrigger>
                    <TabsTrigger value="ai">AI Models</TabsTrigger>
                </TabsList>

                <TabsContent value="general" className="mt-6 flex flex-col gap-6">
                    <Card>
                        <CardHeader>
                            <CardTitle>General Settings</CardTitle>
                            <CardDescription>Manage your workspace preferences.</CardDescription>
                        </CardHeader>
                        <CardContent className="space-y-4">
                            <div className="flex flex-col gap-2 max-w-sm">
                                <Label>Organization Name</Label>
                                <Input
                                    disabled={isOrgLoading}
                                    value={isOrgLoading ? "Loading..." : orgName}
                                    onChange={(e) => setOrgName(e.target.value)}
                                    placeholder={isOrgLoading ? "" : "My Organization"}
                                />
                            </div>
                        </CardContent>
                        <CardFooter className="flex items-center justify-end p-4 px-6 border-t bg-muted/50 rounded-b-lg mt-4">
                            <Button
                                onClick={handleSaveOrganization}
                                disabled={isSavingOrg || isOrgLoading || !orgName.trim() || orgName.trim() === organization?.name}
                            >
                                {isSavingOrg ? "Saving..." : "Save Changes"}
                            </Button>
                        </CardFooter>
                    </Card>

                    <Card className="border-destructive dark:border-red-900/50">
                        <CardHeader>
                            <CardTitle className="text-destructive">Danger Zone</CardTitle>
                            <CardDescription>
                                Irreversible actions regarding your organization. Proceed with extreme caution.
                            </CardDescription>
                        </CardHeader>
                        <CardContent>
                            <div className="flex items-center justify-between">
                                <div className="space-y-1 mr-4">
                                    <h4 className="font-medium text-sm">Delete Organization</h4>
                                    <p className="text-sm text-muted-foreground">
                                        Permanently delete this organization, all of its projects, API keys, and collected error reports. This action cannot be undone.
                                    </p>
                                </div>
                                <Dialog open={isDeleteConfirmOpen} onOpenChange={setIsDeleteConfirmOpen}>
                                    <DialogTrigger asChild>
                                        <Button variant="destructive" className="shrink-0" onClick={() => setIsDeleteConfirmOpen(true)}>
                                            Delete Organization
                                        </Button>
                                    </DialogTrigger>
                                    <DialogContent>
                                        <DialogHeader>
                                            <DialogTitle>Delete Organization</DialogTitle>
                                            <DialogDescription>
                                                Are you absolutely sure you want to delete <span className="font-bold text-foreground">{organization?.name}</span>?
                                                <br /><br />
                                                This will permanently erase all projects, API keys, and collected data. This action is irreversible.
                                            </DialogDescription>
                                        </DialogHeader>
                                        <DialogFooter>
                                            <Button variant="outline" onClick={() => setIsDeleteConfirmOpen(false)} disabled={isDeletingOrg}>
                                                Cancel
                                            </Button>
                                            <Button variant="destructive" onClick={handleDeleteOrganization} disabled={isDeletingOrg}>
                                                {isDeletingOrg ? "Deleting..." : "Yes, delete everything"}
                                            </Button>
                                        </DialogFooter>
                                    </DialogContent>
                                </Dialog>
                            </div>
                        </CardContent>
                    </Card>
                </TabsContent>

                <TabsContent value="members" className="mt-6 flex flex-col gap-6">
                    <MembersManager organizationId={organization?.id} />
                </TabsContent>

                <TabsContent value="teams" className="mt-6 flex flex-col gap-6">
                    <TeamsManager organizationId={organization?.id} />
                </TabsContent>

                <TabsContent value="billing" className="mt-6 flex flex-col gap-6">
                    {subscriptionTier !== "Free" && (
                        <Card className="shadow-sm">
                            <CardHeader className="flex flex-row items-center justify-between">
                                <div>
                                    <CardTitle>Current Subscription</CardTitle>
                                    <CardDescription>You are currently on the <span className="font-semibold">{subscriptionTier}</span> plan.</CardDescription>
                                </div>
                                <Button
                                    onClick={() => handleManageSubscription()}
                                    disabled={isManaging}
                                >
                                    {isManaging ? "Redirecting..." : "Manage Subscription / Update Payment Method"}
                                </Button>
                            </CardHeader>
                        </Card>
                    )}

                    <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-4">
                        <Card className="flex flex-col h-full">
                            <CardHeader>
                                <CardTitle>Cloud Free</CardTitle>
                                <CardDescription>$0 / month</CardDescription>
                            </CardHeader>
                            <CardContent className="flex-1">
                                <ul className="list-disc pl-4 space-y-1 text-sm text-muted-foreground">
                                    <li>Fully Managed Hosting</li>
                                    <li>Up to 1,000 reports / mo</li>
                                    <li>1-Day Log Retention</li>
                                    <li>Community Support</li>
                                </ul>
                            </CardContent>
                            <CardFooter>
                                <Button
                                    className="w-full"
                                    variant={subscriptionTier === "Free" ? "secondary" : "outline"}
                                    onClick={() => handleCheckout("Free")}
                                    disabled={isCheckingOut !== null || subscriptionTier === "Free"}
                                >
                                    {getButtonText("Free", subscriptionTier, isCheckingOut)}
                                </Button>
                            </CardFooter>
                        </Card>
                        <Card className="flex flex-col h-full">
                            <CardHeader>
                                <CardTitle>Starter</CardTitle>
                                <CardDescription>$15 / month</CardDescription>
                            </CardHeader>
                            <CardContent className="flex-1">
                                <ul className="list-disc pl-4 space-y-1 text-sm text-muted-foreground">
                                    <li>Up to 3 members</li>
                                    <li>Basic reporting</li>
                                    <li>Community support</li>
                                </ul>
                            </CardContent>
                            <CardFooter>
                                <Button
                                    className="w-full"
                                    variant={subscriptionTier === "Starter" ? "secondary" : "default"}
                                    onClick={() => handleCheckout("Starter")}
                                    disabled={isCheckingOut !== null || subscriptionTier === "Starter"}
                                >
                                    {getButtonText("Starter", subscriptionTier, isCheckingOut)}
                                </Button>
                            </CardFooter>
                        </Card>

                        <Card className={`relative flex flex-col h-full ${!["Pro", "Enterprise"].includes(subscriptionTier) ? "border-primary" : ""}`}>
                            {!["Pro", "Enterprise"].includes(subscriptionTier) && (
                                <div className="absolute -top-3 left-1/2 -translate-x-1/2 px-3 py-1 bg-primary text-primary-foreground text-xs font-semibold rounded-full">
                                    Recommended
                                </div>
                            )}
                            <CardHeader>
                                <CardTitle>Pro</CardTitle>
                                <CardDescription>$79 / month</CardDescription>
                            </CardHeader>
                            <CardContent className="flex-1">
                                <ul className="list-disc pl-4 space-y-1 text-sm text-muted-foreground">
                                    <li>Unlimited members</li>
                                    <li>Advanced reporting & AI</li>
                                    <li>Priority support</li>
                                </ul>
                            </CardContent>
                            <CardFooter>
                                <Button
                                    className="w-full"
                                    variant={subscriptionTier === "Pro" ? "secondary" : "default"}
                                    onClick={() => handleCheckout("Pro")}
                                    disabled={isCheckingOut !== null || subscriptionTier === "Pro"}
                                >
                                    {getButtonText("Pro", subscriptionTier, isCheckingOut)}
                                </Button>
                            </CardFooter>
                        </Card>

                        <Card className="flex flex-col h-full bg-zinc-950 text-zinc-50 border-zinc-800 dark:bg-zinc-900">
                            <CardHeader>
                                <CardTitle className="text-zinc-50">Enterprise</CardTitle>
                                <CardDescription className="text-zinc-400">Custom Pricing</CardDescription>
                            </CardHeader>
                            <CardContent className="flex-1">
                                <ul className="list-disc pl-4 space-y-1 text-sm text-zinc-400 marker:text-zinc-600">
                                    <li>Dedicated tenant infrastructure</li>
                                    <li>Custom integrations</li>
                                    <li>SLA & Account Manager</li>
                                </ul>
                            </CardContent>
                            <CardFooter>
                                <Button
                                    className={`w-full ${subscriptionTier === "Enterprise" ? "bg-zinc-800 text-zinc-300 hover:bg-zinc-800" : "bg-white text-zinc-950 hover:bg-zinc-200"}`}
                                    onClick={() => window.location.href = "mailto:sales@winnowtriage.com?subject=Enterprise%20Plan%20Inquiry"}
                                    disabled={subscriptionTier === "Enterprise"}
                                >
                                    {subscriptionTier === "Enterprise" ? "Current Plan" : "Contact Sales / Upgrade"}
                                </Button>
                            </CardFooter>
                        </Card>
                    </div>
                </TabsContent>



                <TabsContent value="ai" className="mt-6">
                    <Card>
                        <CardHeader>
                            <CardTitle>AI Configuration</CardTitle>
                            <CardDescription>Configure LLM providers and models.</CardDescription>
                        </CardHeader>
                        <CardContent>
                            <p className="text-sm text-muted-foreground">AI settings are currently managed via appsettings.json on the server.</p>
                        </CardContent>
                    </Card>
                </TabsContent>
            </Tabs>
        </div>
    );
}

function TeamsManager({ organizationId }: { organizationId?: string }) {
    const { projects, refreshProjects, setOrgWide } = useProject();
    const [selectedTeamId, setSelectedTeamId] = useState<string | null>(null);
    const [memberSelects, setMemberSelects] = useState<Record<string, string>>({});

    React.useEffect(() => {
        setOrgWide(true);
        return () => setOrgWide(false);
    }, [setOrgWide]);
    const { data: teams, isLoading, refetch } = useQuery<{ id: string, name: string, createdAt: string, projectCount: number, members: { userId: string, fullName: string }[], projects: { id: string, name: string }[] }[]>({
        queryKey: ['teams', organizationId],
        queryFn: async () => {
            const { data } = await api.get('/teams');
            return data;
        },
        enabled: !!organizationId
    });

    const { data: orgMembers } = useQuery<{ userId: string, fullName: string, email: string, role: string }[]>({
        queryKey: ['org-members', organizationId],
        queryFn: async () => {
            const { data } = await api.get('/organizations/current/members');
            return data;
        },
        enabled: !!organizationId
    });

    const [newTeamName, setNewTeamName] = useState("");
    const [isCreating, setIsCreating] = useState(false);

    const handleCreateTeam = async () => {
        if (!newTeamName.trim()) return;
        setIsCreating(true);
        try {
            await api.post('/teams', { name: newTeamName.trim() });
            setNewTeamName("");
            await refetch();
            toast.success("Team created successfully");
        } catch (error) {
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
        } catch (error) {
            toast.error("Failed to delete team");
        }
    };

    const handleAssignProject = async (projectId: string, teamId: string | null) => {
        const project = projects.find(p => p.id === projectId);
        if (!project) return;

        try {
            await api.put(`/projects/${projectId}`, {
                name: project.name,
                teamId: teamId === "none" ? null : teamId
            });
            await refreshProjects();
            await refetch();
            toast.success(teamId ? "Project assigned to team" : "Project unassigned from team");
        } catch (error) {
            toast.error("Failed to update project assignment");
        }
    };

    const handleAddTeamMember = async (teamId: string, userId: string) => {
        if (!userId) return;
        setMemberSelects(prev => ({ ...prev, [teamId]: userId }));
        try {
            await api.post(`/teams/${teamId}/members`, { userId });
            await refetch();
            toast.success("Member added to team");
            setMemberSelects(prev => ({ ...prev, [teamId]: "" }));
        } catch (error) {
            toast.error("Failed to add member to team");
        }
    };

    const handleRemoveTeamMember = async (teamId: string, userId: string) => {
        try {
            await api.delete(`/teams/${teamId}/members/${userId}`);
            await refetch();
            toast.success("Member removed from team");
        } catch (error) {
            toast.error("Failed to remove member from team");
        }
    };

    if (isLoading) return <div>Loading teams...</div>;

    const selectedTeam = teams?.find(t => t.id === selectedTeamId);

    const formatCount = (count: number, label: string) => {
        return `${count} ${label}${count === 1 ? '' : 's'}`;
    };

    return (
        <Card>
            <CardHeader>
                <CardTitle>Teams</CardTitle>
                <CardDescription>Manage teams within your organization. Projects can be assigned to teams for better organization.</CardDescription>
            </CardHeader>
            <CardContent className="space-y-6">
                <div className="flex gap-2">
                    <Input
                        placeholder="New team name..."
                        value={newTeamName}
                        onChange={(e) => setNewTeamName(e.target.value)}
                        className="max-w-sm"
                    />
                    <Button onClick={handleCreateTeam} disabled={isCreating || !newTeamName.trim()}>
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
                                    <h3 className="font-semibold group-hover:text-primary transition-colors">{team.name}</h3>
                                    <Settings2 className="h-4 w-4 text-muted-foreground opacity-0 group-hover:opacity-100 transition-opacity" />
                                </div>
                                <div className="flex items-center gap-3 text-xs text-muted-foreground">
                                    <span className="flex items-center gap-1">
                                        <Users className="h-3 w-3" />
                                        {formatCount(team.members?.length || 0, "member")}
                                    </span>
                                    <span>•</span>
                                    <span>
                                        {formatCount(team.projectCount, "project")}
                                    </span>
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
                        projects.filter((p: Project) => !p.teamId).map((project: Project) => (
                            <div key={project.id} className="flex items-center justify-between p-3 border rounded-md">
                                <span className="text-sm font-medium">{project.name}</span>
                                <Select onValueChange={(value: string) => handleAssignProject(project.id, value)}>
                                    <SelectTrigger className="h-8 w-[180px] text-xs">
                                        <SelectValue placeholder="Assign to team..." />
                                    </SelectTrigger>
                                    <SelectContent>
                                        {teams?.map((team: any) => (
                                            <SelectItem key={team.id} value={team.id}>{team.name}</SelectItem>
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
                    onRemoveMember={(userId) => handleRemoveTeamMember(selectedTeamId!, userId)}
                    onDeleteTeam={() => {
                        handleDeleteTeam(selectedTeamId!);
                        setSelectedTeamId(null);
                    }}
                    onUnassignProject={(projectId) => handleAssignProject(projectId, "none")}
                    memberSelectValue={selectedTeamId ? (memberSelects[selectedTeamId] || "") : ""}
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
    memberSelectValue
}: {
    team: any,
    onClose: () => void,
    orgMembers: any[],
    projects: any[],
    onAddMember: (userId: string) => void,
    onRemoveMember: (userId: string) => void,
    onDeleteTeam: () => void,
    onUnassignProject: (projectId: string) => void,
    memberSelectValue: string
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
                                        {orgMembers.filter(om => !(team?.members || []).some((tm: any) => tm.userId === om.userId)).map(member => (
                                            <SelectItem key={member.userId} value={member.userId}>
                                                {member.fullName}
                                            </SelectItem>
                                        ))}
                                        {orgMembers.filter(om => !(team?.members || []).some((tm: any) => tm.userId === om.userId)).length === 0 && (
                                            <div className="p-2 text-xs text-muted-foreground text-center">All members already in team</div>
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
                                                <TableCell colSpan={3} className="h-24 text-center text-muted-foreground italic">
                                                    No members in this team.
                                                </TableCell>
                                            </TableRow>
                                        ) : (
                                            team?.members.map((member: any) => (
                                                <TableRow key={member.userId} className="group">
                                                    <TableCell>
                                                        <div className="flex items-center gap-3">
                                                            <Avatar className="h-8 w-8">
                                                                <AvatarFallback className="text-[10px] bg-primary/10 text-primary">
                                                                    {member.fullName.split(' ').map((n: string) => n[0]).join('').toUpperCase()}
                                                                </AvatarFallback>
                                                            </Avatar>
                                                            <div className="flex flex-col">
                                                                <span className="font-medium text-sm">{member.fullName}</span>
                                                                {/* Example email placeholder since it's not in the team response currently */}
                                                                <span className="text-[10px] text-muted-foreground truncate max-w-[120px]">
                                                                    {orgMembers.find(om => om.userId === member.userId)?.email || "n/a"}
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
                                                                <Button variant="ghost" size="sm" className="h-8 w-8 p-0">
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
                                        <p className="text-sm text-muted-foreground">No projects assigned to this team.</p>
                                    </div>
                                ) : (
                                    projects.map((project) => (
                                        <div key={project.id} className="flex items-center justify-between p-3 border rounded-lg bg-muted/5">
                                            <span className="text-sm font-medium">{project.name}</span>
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
                                    <h4 className="text-sm font-bold text-destructive uppercase tracking-tight">Danger Zone</h4>
                                    <p className="text-xs text-muted-foreground">This action will permanently remove the team. Projects will be unassigned but not deleted.</p>
                                </div>

                                <AlertDialog>
                                    <AlertDialogTrigger asChild>
                                        <Button
                                            variant="destructive"
                                            size="sm"
                                            className="w-full"
                                        >
                                            Delete Team
                                        </Button>
                                    </AlertDialogTrigger>
                                    <AlertDialogContent>
                                        <AlertDialogHeader>
                                            <AlertDialogTitle>Are you absolutely sure?</AlertDialogTitle>
                                            <AlertDialogDescription>
                                                This will permanently delete the <span className="font-bold text-foreground">{team?.name}</span> team.
                                                Assigned projects will be unassigned but not deleted. This action cannot be undone.
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



function MembersManager({ organizationId }: { organizationId?: string }) {
    const { data: teams } = useQuery<{ id: string, name: string, members: { userId: string }[] }[]>({
        queryKey: ['teams', organizationId],
        queryFn: async () => {
            const { data } = await api.get('/teams');
            return data;
        },
        enabled: !!organizationId
    });

    const { data: orgMembers, isLoading } = useQuery<{ userId: string, fullName: string, email: string, role: string, JoinedAt?: string }[]>({
        queryKey: ['org-members', organizationId],
        queryFn: async () => {
            const { data } = await api.get('/organizations/current/members');
            return data;
        },
        enabled: !!organizationId
    });

    const [isInviteModalOpen, setIsInviteModalOpen] = useState(false);

    if (isLoading) return <div className="p-8 text-center text-muted-foreground">Loading members...</div>;

    // Helper to get teams for a user
    const getUserTeams = (userId: string) => {
        if (!teams) return [];
        return teams.filter(t => t.members.some(m => m.userId === userId));
    };

    const formatTeamList = (teams: any[]) => {
        if (teams.length === 0) return "No teams";
        if (teams.length === 1) return teams[0].name;
        return `${teams[0].name}, +${teams.length - 1} more`;
    };

    return (
        <Card>
            <CardHeader className="flex flex-row items-center justify-between space-y-0">
                <div>
                    <CardTitle>Organization Directory</CardTitle>
                    <CardDescription>Manage your organization's members and their global roles.</CardDescription>
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
                            {orgMembers?.map((member) => {
                                const userTeams = getUserTeams(member.userId);
                                return (
                                    <TableRow key={member.userId}>
                                        <TableCell>
                                            <div className="flex items-center gap-3">
                                                <Avatar className="h-9 w-9">
                                                    <AvatarFallback className="bg-primary/10 text-primary text-xs">
                                                        {member.fullName.split(' ').map((n: string) => n[0]).join('').toUpperCase()}
                                                    </AvatarFallback>
                                                </Avatar>
                                                <div className="flex flex-col">
                                                    <span className="font-semibold text-sm">{member.fullName}</span>
                                                    <span className="text-xs text-muted-foreground">{member.email}</span>
                                                </div>
                                            </div>
                                        </TableCell>
                                        <TableCell>
                                            <span className="text-sm">
                                                {member.role === 'owner' ? 'Org Owner' : member.role}
                                            </span>
                                        </TableCell>
                                        <TableCell>
                                            <div className="flex flex-col">
                                                <span className="text-sm">{formatTeamList(userTeams)}</span>
                                                {userTeams.length > 0 && (
                                                    <span className="text-[10px] text-muted-foreground">
                                                        {userTeams.length} {userTeams.length === 1 ? 'team' : 'teams'}
                                                    </span>
                                                )}
                                            </div>
                                        </TableCell>
                                        <TableCell>
                                            <div className="flex items-center gap-1.5">
                                                <div className="h-1.5 w-1.5 rounded-full bg-green-500" />
                                                <span className="text-xs font-medium">Active</span>
                                            </div>
                                        </TableCell>
                                        <TableCell className="text-right">
                                            <DropdownMenu>
                                                <DropdownMenuTrigger asChild>
                                                    <Button variant="ghost" size="sm" className="h-8 w-8 p-0">
                                                        <MoreHorizontal className="h-4 w-4" />
                                                    </Button>
                                                </DropdownMenuTrigger>
                                                <DropdownMenuContent align="end">
                                                    <DropdownMenuItem>Edit Role</DropdownMenuItem>
                                                    <DropdownMenuItem className="text-destructive focus:text-destructive">
                                                        Remove from Organization
                                                    </DropdownMenuItem>
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
                />
            </CardContent>
        </Card>
    );
}

function InviteMemberModal({ isOpen, onClose, organizationId }: { isOpen: boolean, onClose: () => void, organizationId?: string }) {
    const [email, setEmail] = useState("");
    const [role, setRole] = useState("Member");
    const [isInviting, setIsInviting] = useState(false);

    const handleInvite = async () => {
        if (!email.trim() || !organizationId) return;
        setIsInviting(true);
        try {
            await api.post(`/organizations/${organizationId}/invitations`, {
                orgId: organizationId,
                email: email.trim(),
                role: role
            });
            toast.success(`Invite sent to ${email}`);
            onClose();
            setEmail("");
        } catch (error) {
            console.error("Failed to send invite:", error);
            toast.error("Failed to send invitation. Please try again.");
        } finally {
            setIsInviting(false);
        }
    };

    return (
        <Dialog open={isOpen} onOpenChange={onClose}>
            <DialogContent className="sm:max-w-[425px]">
                <DialogHeader>
                    <DialogTitle>Invite Member</DialogTitle>
                    <DialogDescription>
                        Send an invitation to join your organization and assign an initial role.
                    </DialogDescription>
                </DialogHeader>
                <div className="space-y-4 py-4">
                    <div className="space-y-2">
                        <Label htmlFor="email">Email Address</Label>
                        <Input
                            id="email"
                            placeholder="colleague@company.com"
                            value={email}
                            onChange={(e) => setEmail(e.target.value)}
                        />
                    </div>
                    <div className="space-y-2">
                        <Label htmlFor="role">Initial Role</Label>
                        <Select value={role} onValueChange={setRole}>
                            <SelectTrigger id="role w-full">
                                <SelectValue placeholder="Select a role" />
                            </SelectTrigger>
                            <SelectContent>
                                <SelectItem value="owner">Owner</SelectItem>
                                <SelectItem value="Admin">Admin</SelectItem>
                                <SelectItem value="Member">Member</SelectItem>
                            </SelectContent>
                        </Select>
                    </div>
                </div>
                <DialogFooter>
                    <Button variant="outline" onClick={onClose} disabled={isInviting}>
                        Cancel
                    </Button>
                    <Button onClick={handleInvite} disabled={isInviting || !email.trim()}>
                        {isInviting ? "Sending..." : "Send Invite"}
                    </Button>
                </DialogFooter>
            </DialogContent>
        </Dialog>
    );
}

