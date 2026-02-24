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
import { Trash2, Users, UserPlus, X } from 'lucide-react';

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
                <TabsList className="grid w-full grid-cols-4 max-w-[600px]">
                    <TabsTrigger value="general">General</TabsTrigger>
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
    const { projects, refreshProjects } = useProject();
    const [memberSelects, setMemberSelects] = useState<Record<string, string>>({});
    const { data: teams, isLoading, refetch } = useQuery<{ id: string, name: string, createdAt: string, projectCount: number, members: { userId: string, fullName: string }[] }[]>({
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
    const [isDeleting, setIsDeleting] = useState<string | null>(null);

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
        setIsDeleting(id);
        try {
            await api.delete(`/teams/${id}`);
            await refetch();
            await refreshProjects();
            toast.success("Team deleted successfully");
        } catch (error) {
            toast.error("Failed to delete team");
        } finally {
            setIsDeleting(null);
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

                <div className="space-y-4">
                    {teams?.length === 0 ? (
                        <div className="p-8 text-center text-muted-foreground text-sm border rounded-md">
                            No teams found. Create one to get started.
                        </div>
                    ) : (
                        teams?.map((team) => (
                            <div key={team.id} className="border rounded-md overflow-hidden">
                                <div className="flex items-center justify-between p-4 bg-muted/30 border-b">
                                    <div>
                                        <h3 className="font-semibold">{team.name}</h3>
                                        <p className="text-xs text-muted-foreground">{projects.filter((p: Project) => p.teamId === team.id).length} projects assigned</p>
                                    </div>
                                    <Button
                                        variant="ghost"
                                        size="sm"
                                        className="text-destructive hover:text-destructive hover:bg-destructive/10"
                                        onClick={() => handleDeleteTeam(team.id)}
                                        disabled={isDeleting === team.id}
                                    >
                                        <Trash2 className="h-4 w-4" />
                                    </Button>
                                </div>
                                <div className="p-4 space-y-2">
                                    {projects.filter((p: Project) => p.teamId === team.id).map((project: Project) => (
                                        <div key={project.id} className="flex items-center justify-between text-sm py-1">
                                            <span>{project.name}</span>
                                            <Button variant="ghost" size="sm" onClick={() => handleAssignProject(project.id, "none")} className="h-6 px-2 text-xs">
                                                Unassign
                                            </Button>
                                        </div>
                                    ))}
                                    {projects.filter((p: Project) => p.teamId === team.id).length === 0 && (
                                        <p className="text-xs text-muted-foreground italic">No projects assigned to this team.</p>
                                    )}
                                </div>

                                <div className="p-4 border-t bg-muted/5">
                                    <div className="flex items-center justify-between mb-3">
                                        <h4 className="text-sm font-medium flex items-center gap-2">
                                            <Users className="h-4 w-4 text-muted-foreground" />
                                            Team Members
                                        </h4>
                                        <Select
                                            value={memberSelects[team.id] || ""}
                                            onValueChange={(userId) => handleAddTeamMember(team.id, userId)}
                                        >
                                            <SelectTrigger className="h-7 w-[160px] text-xs">
                                                <UserPlus className="h-3 w-3 mr-1" />
                                                <span>Add Member...</span>
                                            </SelectTrigger>
                                            <SelectContent>
                                                {orgMembers?.filter(om => !(team.members || []).some(tm => tm.userId === om.userId)).map(member => (
                                                    <SelectItem key={member.userId} value={member.userId}>
                                                        {member.fullName}
                                                    </SelectItem>
                                                ))}
                                                {orgMembers?.filter(om => !(team.members || []).some(tm => tm.userId === om.userId)).length === 0 && (
                                                    <div className="p-2 text-xs text-muted-foreground text-center">All members already in team</div>
                                                )}
                                            </SelectContent>
                                        </Select>
                                    </div>
                                    <div className="flex flex-wrap gap-2">
                                        {(team.members || []).map(member => (
                                            <div key={member.userId} className="flex items-center gap-1.5 px-2 py-1 bg-background border rounded-full text-xs">
                                                <span>{member.fullName}</span>
                                                <button
                                                    onClick={() => handleRemoveTeamMember(team.id, member.userId)}
                                                    className="hover:text-destructive text-muted-foreground"
                                                >
                                                    <X className="h-3 w-3" />
                                                </button>
                                            </div>
                                        ))}
                                        {(team.members || []).length === 0 && (
                                            <p className="text-xs text-muted-foreground italic">No members in this team.</p>
                                        )}
                                    </div>
                                </div>
                            </div>
                        ))
                    )}
                </div>

                <div className="pt-6 border-t font-semibold">
                    <h3>Unassigned Projects</h3>
                </div>

                <div className="border rounded-md divide-y">
                    {projects.filter((p: Project) => !p.teamId).length === 0 ? (
                        <div className="p-4 text-center text-muted-foreground text-sm">
                            All projects are assigned to teams.
                        </div>
                    ) : (
                        projects.filter((p: Project) => !p.teamId).map((project: Project) => (
                            <div key={project.id} className="flex items-center justify-between p-4">
                                <span className="text-sm font-medium">{project.name}</span>
                                <div className="flex items-center gap-2">
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
                            </div>
                        ))
                    )}
                </div>
            </CardContent>
        </Card>
    );
}



