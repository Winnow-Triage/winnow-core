import React, { useState } from "react";
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
  CardFooter,
} from "@/components/ui/card";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog";
import { toast } from "sonner";
import { useNavigate, useSearchParams } from "react-router-dom";
import { PermissionGate } from "@/components/PermissionGate";
import { useProject } from "@/context/ProjectContext";
import type { Project } from "@/context/ProjectContext";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Users, UserPlus, MoreHorizontal, Settings2, Loader2, AlertCircle } from "lucide-react";
import { PageTitle } from "@/components/ui/page-title";
import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetHeader,
  SheetTitle,
} from "@/components/ui/sheet";
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
import { ScrollArea } from "@/components/ui/scroll-area";
import { Progress } from "@/components/ui/progress";
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
import { RolesManager } from "@/components/settings/RolesManager";

export default function Settings() {
  const [searchParams, setSearchParams] = useSearchParams();
  const currentTab = searchParams.get("tab") || "general";

  const handleTabChange = (value: string) => {
    setSearchParams({ tab: value });
  };

  const [isCheckingOut, setIsCheckingOut] = useState<string | null>(null);
  const [isManaging, setIsManaging] = useState(false);

  const {
    data: organization,
    isLoading: isOrgLoading,
    error: orgError,
    refetch,
  } = useQuery<{ id: string; name: string; subscriptionTier: string }>({
    queryKey: ["current-organization"],
    queryFn: async () => {
      const { data } = await api.get("/organizations/current");
      return data;
    },
    retry: 0,
  });

  const { data: billingStatus, isLoading: isBillingLoading } = useQuery({
    queryKey: ["billing-status"],
    queryFn: async () => {
      const { getBillingStatus } = await import("@/lib/api");
      return getBillingStatus();
    },
    retry: 0,
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
      await api.put("/organizations/current", { name: orgName.trim() });
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
      await api.delete("/organizations/current");
      // Logout user since org no longer exists
      const { logoutUser } = await import("@/lib/api");
      await logoutUser();
      toast.success("Organization deleted. You have been logged out.");
      navigate("/login");
    } catch (error) {
      console.error("Failed to delete organization:", error);
      toast.error("Failed to delete organization. Please contact support.");
    } finally {
      setIsDeletingOrg(false);
      setIsDeleteConfirmOpen(false);
    }
  };

  const subscriptionTier: string = organization?.subscriptionTier || "Free";

  const getButtonText = (
    targetTier: string,
    currentTier: string,
    checkingOut: string | null,
  ) => {
    if (checkingOut === targetTier) return "Redirecting...";
    if (currentTier === targetTier) return "Current Plan";

    const tiers = ["Free", "Starter", "Pro", "Enterprise"];
    const currentIndex = tiers.indexOf(currentTier);
    const targetIndex = tiers.indexOf(targetTier);

    if (
      currentIndex !== -1 &&
      targetIndex !== -1 &&
      targetIndex < currentIndex
    ) {
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
    if (
      actionText.includes("Downgrade") ||
      (subscriptionTier !== "Free" && actionText.includes("Upgrade"))
    ) {
      await handleManageSubscription(tier === "Free" ? "cancel" : "update");
      setIsCheckingOut(null);
      return;
    }

    try {
      const { data } = await api.post("/billing/checkout", {
        targetTier: tier,
      });
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
      const { data } = await api.post("/billing/portal", {
        action: action ?? null,
      });
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

  if (isOrgLoading) {
    return (
      <div className="flex h-screen items-center justify-center">
        <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
      </div>
    );
  }

  if (orgError) {
    return (
      <div className="flex flex-col items-center justify-center min-h-[60vh] text-center p-8">
        <AlertCircle className="h-12 w-12 text-destructive mb-4" />
        <h3 className="text-xl font-bold">Access Denied</h3>
        <p className="text-muted-foreground mt-2 max-w-md">
          {(orgError as any).response?.data?.detail || 
           (orgError as any).response?.data?.message || 
           "You don't have permission to view organization settings."}
        </p>
      </div>
    );
  }

  return (
    <div className="max-w-4xl w-full mx-auto">
      <div className="flex flex-col gap-1 mb-8">
        <PageTitle>Organization Settings</PageTitle>
        <p className="text-muted-foreground">
          Manage settings and access for{" "}
          {organization?.name || "your organization"}
        </p>
      </div>

      <Tabs
        value={currentTab}
        onValueChange={handleTabChange}
        className="w-full"
      >
        <TabsList className="grid w-full grid-cols-6 max-w-[800px]">
          <TabsTrigger value="general">General</TabsTrigger>
          <TabsTrigger value="members">Members</TabsTrigger>
          <TabsTrigger value="teams">Teams</TabsTrigger>
          <PermissionGate permission="roles:manage">
            <TabsTrigger value="roles">Roles</TabsTrigger>
          </PermissionGate>
          <TabsTrigger value="billing">Billing</TabsTrigger>
          <TabsTrigger value="ai">AI Models</TabsTrigger>
        </TabsList>

        <TabsContent value="general" className="mt-6 flex flex-col gap-6">
          <Card>
            <CardHeader>
              <CardTitle>General Settings</CardTitle>
              <CardDescription>
                Manage your workspace preferences.
              </CardDescription>
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
            <CardFooter className="flex items-center justify-end p-4 px-6 border-t bg-muted/50 rounded-b-3xl mt-4">
              <Button
                onClick={handleSaveOrganization}
                disabled={
                  isSavingOrg ||
                  isOrgLoading ||
                  !orgName.trim() ||
                  orgName.trim() === organization?.name
                }
              >
                {isSavingOrg ? "Saving..." : "Save Changes"}
              </Button>
            </CardFooter>
          </Card>

          <Card className="border-destructive dark:border-red-900/50">
            <CardHeader>
              <CardTitle className="text-destructive">Danger Zone</CardTitle>
              <CardDescription>
                Irreversible actions regarding your organization. Proceed with
                extreme caution.
              </CardDescription>
            </CardHeader>
            <CardContent>
              <div className="flex items-center justify-between">
                <div className="space-y-1 mr-4">
                  <h4 className="font-medium text-sm">Delete Organization</h4>
                  <p className="text-sm text-muted-foreground">
                    Permanently delete this organization, all of its projects,
                    API keys, and collected error reports. This action cannot be
                    undone.
                  </p>
                </div>
                <Dialog
                  open={isDeleteConfirmOpen}
                  onOpenChange={setIsDeleteConfirmOpen}
                >
                  <DialogTrigger asChild>
                    <Button
                      variant="destructive"
                      className="shrink-0"
                      onClick={() => setIsDeleteConfirmOpen(true)}
                    >
                      Delete Organization
                    </Button>
                  </DialogTrigger>
                  <DialogContent>
                    <DialogHeader>
                      <DialogTitle>Delete Organization</DialogTitle>
                      <DialogDescription>
                        Are you absolutely sure you want to delete{" "}
                        <span className="font-bold text-foreground">
                          {organization?.name}
                        </span>
                        ?
                        <br />
                        <br />
                        This will permanently erase all projects, API keys, and
                        collected data. This action is irreversible.
                      </DialogDescription>
                    </DialogHeader>
                    <DialogFooter>
                      <Button
                        variant="outline"
                        onClick={() => setIsDeleteConfirmOpen(false)}
                        disabled={isDeletingOrg}
                      >
                        Cancel
                      </Button>
                      <Button
                        variant="destructive"
                        onClick={handleDeleteOrganization}
                        disabled={isDeletingOrg}
                      >
                        {isDeletingOrg
                          ? "Deleting..."
                          : "Yes, delete everything"}
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

        <PermissionGate permission="roles:manage">
          <TabsContent value="roles" className="mt-6 flex flex-col gap-6">
            <RolesManager organizationId={organization?.id} />
          </TabsContent>
        </PermissionGate>

        <TabsContent value="billing" className="mt-6 flex flex-col gap-6">
          {subscriptionTier !== "Free" && (
            <Card className="shadow-sm">
              <CardHeader className="flex flex-row items-center justify-between">
                <div>
                  <CardTitle>Current Subscription</CardTitle>
                  <CardDescription>
                    You are currently on the{" "}
                    <span className="font-semibold">{subscriptionTier}</span>{" "}
                    plan.
                  </CardDescription>
                </div>
                <Button
                  onClick={() => handleManageSubscription()}
                  disabled={isManaging}
                >
                  {isManaging
                    ? "Redirecting..."
                    : "Manage Subscription / Update Payment Method"}
                </Button>
              </CardHeader>
            </Card>
          )}

          {/* Progress Bar Component */}
          {!isBillingLoading && billingStatus && (
            <Card className="shadow-sm">
              <CardHeader>
                <CardTitle>Monthly Usage</CardTitle>
                <CardDescription>
                  You have used {billingStatus.reportsUsedThisMonth} report
                  {billingStatus.reportsUsedThisMonth === 1 ? "" : "s"} this
                  month.
                  {billingStatus.reportLimit === null &&
                    " You have unlimited report ingestion."}
                  {billingStatus.reportLimit !== null &&
                    ` Your limit is ${billingStatus.reportLimit}.`}
                </CardDescription>
              </CardHeader>
              <CardContent>
                {billingStatus.reportLimit !== null ? (
                  <div className="space-y-2">
                    <Progress
                      value={Math.min(
                        100,
                        Math.max(
                          0,
                          (billingStatus.reportsUsedThisMonth /
                            billingStatus.reportLimit) *
                          100,
                        ),
                      )}
                      className="h-4"
                      indicatorColor={
                        billingStatus.reportsUsedThisMonth /
                          billingStatus.reportLimit >=
                          2
                          ? "bg-red-500 shadow-[0_0_12px_rgba(239,68,68,0.8)]"
                          : billingStatus.reportsUsedThisMonth /
                            billingStatus.reportLimit >=
                            1
                            ? "bg-amber-500 shadow-[0_0_12px_rgba(245,158,11,0.8)] animate-pulse"
                            : "bg-blue-500 shadow-[0_0_12px_rgba(59,130,246,0.5)]"
                      }
                    />
                    <div className="flex justify-between text-xs text-muted-foreground w-full">
                      <span>0</span>
                      <span>{billingStatus.reportLimit} Limit</span>
                    </div>
                  </div>
                ) : (
                  <div className="flex items-center gap-2 text-sm text-green-600 dark:text-green-400">
                    <div className="h-2 w-full bg-green-500/20 rounded-full overflow-hidden">
                      <div className="h-full bg-green-500 w-full" />
                    </div>
                  </div>
                )}

                <div className="mt-8 border-t pt-6">
                  <h4 className="text-sm font-semibold mb-2">AI Summaries</h4>
                  {billingStatus.monthlySummaryLimit === null ? (
                    <div className="space-y-2">
                      <div className="flex items-center gap-2 text-sm text-green-600 dark:text-green-400">
                        <div className="h-2 w-full bg-green-500/20 rounded-full overflow-hidden">
                          <div className="h-full bg-green-500 w-full" />
                        </div>
                      </div>
                      <div className="flex justify-between text-xs text-muted-foreground w-full">
                        <span>
                          {billingStatus.currentMonthSummaries} generated
                        </span>
                        <span>Unlimited</span>
                      </div>
                    </div>
                  ) : billingStatus.monthlySummaryLimit > 0 ? (
                    <div className="space-y-2">
                      <Progress
                        value={Math.min(
                          100,
                          Math.max(
                            0,
                            (billingStatus.currentMonthSummaries /
                              (billingStatus.monthlySummaryLimit || 1)) *
                            100,
                          ),
                        )}
                        className="h-4"
                        indicatorColor={
                          billingStatus.currentMonthSummaries /
                            (billingStatus.monthlySummaryLimit || 1) >=
                            1
                            ? "bg-amber-500 shadow-[0_0_12px_rgba(245,158,11,0.8)] animate-pulse"
                            : billingStatus.currentMonthSummaries /
                              (billingStatus.monthlySummaryLimit || 1) >=
                              0.8
                              ? "bg-amber-400 shadow-[0_0_12px_rgba(251,191,36,0.6)]"
                              : "bg-purple-500 shadow-[0_0_12px_rgba(168,85,247,0.5)]"
                        }
                      />
                      <div className="flex justify-between text-xs text-muted-foreground w-full">
                        <span>
                          {billingStatus.currentMonthSummaries} generated
                        </span>
                        <span>{billingStatus.monthlySummaryLimit} Limit</span>
                      </div>
                    </div>
                  ) : (
                    <div className="flex justify-between text-xs text-muted-foreground">
                      <span>No AI Summaries generated.</span>
                      {subscriptionTier === "Free" ? (
                        <span>
                          Upgrade to Starter or Pro to enable AI triage.
                        </span>
                      ) : (
                        <span>
                          AI Triage plan limit reached or disabled.
                        </span>
                      )}
                    </div>
                  )}
                </div>
              </CardContent>
              {billingStatus.reportLimit !== null &&
                billingStatus.reportsUsedThisMonth >=
                billingStatus.reportLimit && (
                  <CardFooter className="bg-destructive/10 text-destructive text-sm p-4 rounded-b-3xl border-t border-destructive/20">
                    You have reached your monthly ingestion limit. New reports
                    will be rejected until you upgrade your plan or until the
                    next billing cycle.
                  </CardFooter>
                )}
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
                  variant={
                    subscriptionTier === "Free" ? "secondary" : "outline"
                  }
                  onClick={() => handleCheckout("Free")}
                  disabled={
                    isCheckingOut !== null || subscriptionTier === "Free"
                  }
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
                  variant={
                    subscriptionTier === "Starter" ? "secondary" : "default"
                  }
                  onClick={() => handleCheckout("Starter")}
                  disabled={
                    isCheckingOut !== null || subscriptionTier === "Starter"
                  }
                >
                  {getButtonText("Starter", subscriptionTier, isCheckingOut)}
                </Button>
              </CardFooter>
            </Card>

            <Card
              className={`relative flex flex-col h-full ${!["Pro", "Enterprise"].includes(subscriptionTier) ? "border-primary" : ""}`}
            >
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
                  disabled={
                    isCheckingOut !== null || subscriptionTier === "Pro"
                  }
                >
                  {getButtonText("Pro", subscriptionTier, isCheckingOut)}
                </Button>
              </CardFooter>
            </Card>

            <Card className="flex flex-col h-full bg-zinc-950 text-zinc-50 border-zinc-800 dark:bg-zinc-900">
              <CardHeader>
                <CardTitle className="text-zinc-50">Enterprise</CardTitle>
                <CardDescription className="text-zinc-400">
                  Custom Pricing
                </CardDescription>
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
                  onClick={() =>
                  (window.location.href =
                    "mailto:sales@winnowtriage.com?subject=Enterprise%20Plan%20Inquiry")
                  }
                  disabled={subscriptionTier === "Enterprise"}
                >
                  {subscriptionTier === "Enterprise"
                    ? "Current Plan"
                    : "Contact Sales / Upgrade"}
                </Button>
              </CardFooter>
            </Card>
          </div>
        </TabsContent>

        <TabsContent value="ai" className="mt-6">
          <Card>
            <CardHeader>
              <CardTitle>AI Configuration</CardTitle>
              <CardDescription>
                Configure LLM providers and models.
              </CardDescription>
            </CardHeader>
            <CardContent>
              <p className="text-sm text-muted-foreground">
                AI settings are currently managed via appsettings.json on the
                server.
              </p>
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
  const [memberSelects, setMemberSelects] = useState<Record<string, string>>(
    {},
  );

  React.useEffect(() => {
    setOrgWide(true);
    return () => setOrgWide(false);
  }, [setOrgWide]);
  const {
    data: teams,
    isLoading,
    error,
    refetch,
  } = useQuery<
    {
      id: string;
      name: string;
      createdAt: string;
      projectCount: number;
      members: { userId: string; fullName: string }[];
      projects: { id: string; name: string }[];
    }[]
  >({
    queryKey: ["teams", organizationId],
    queryFn: async () => {
      const { data } = await api.get("/teams");
      return data;
    },
    enabled: !!organizationId,
    retry: 0,
  });

  const { data: orgMembers } = useQuery<
    {
      id: string;
      fullName: string | null;
      email: string;
      globalRole: string;
      status: string;
    }[]
  >({
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

  const handleAssignProject = async (
    projectId: string,
    teamId: string | null,
  ) => {
    const project = projects.find((p) => p.id === projectId);
    if (!project) return;

    try {
      await api.put(`/projects/${projectId}`, {
        name: project.name,
        teamId: teamId === "none" ? null : teamId,
      });
      await refreshProjects();
      await refetch();
      toast.success(
        teamId ? "Project assigned to team" : "Project unassigned from team",
      );
    } catch (error) {
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
          {(error as any).response?.data?.detail || "You don't have permission to manage teams."}
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
                      {teams?.map((team: any) => (
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
  team: any;
  onClose: () => void;
  orgMembers: any[];
  projects: any[];
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
                            (tm: any) => tm.userId === om.id,
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
                          (tm: any) => tm.userId === om.id,
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
                      team?.members.map((member: any) => (
                        <TableRow key={member.userId} className="group">
                          <TableCell>
                            <div className="flex items-center gap-3">
                              <Avatar className="h-8 w-8">
                                <AvatarFallback className="text-[10px] bg-primary/10 text-primary">
                                  {member.fullName
                                    .split(" ")
                                    .map((n: string) => n[0])
                                    .join("")
                                    .toUpperCase()}
                                </AvatarFallback>
                              </Avatar>
                              <div className="flex flex-col">
                                <span className="font-medium text-sm">
                                  {member.fullName}
                                </span>
                                {/* Example email placeholder since it's not in the team response currently */}
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

interface Role {
  id: string;
  name: string;
  // Add other properties if known, e.g., description, permissions
}

function MembersManager({ organizationId }: { organizationId?: string }) {
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
  } = useQuery<
    {
      id: string;
      fullName: string | null;
      email: string;
      globalRole: string;
      roleId: string;
      status: string;
      isLocked: boolean;
      joinedAt?: string;
    }[]
  >({
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
    } catch (error: any) {
      toast.error(
        error.response?.data?.errors?.GeneralErrors?.[0] ||
        error.response?.data?.errors?.generalErrors?.[0] ||
        error.response?.data?.message || 
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
    } catch (error) {
      toast.error("Failed to resend invitation");
    }
  };

  const handleCancelInvitation = async (invitationId: string) => {
    if (!organizationId) return;
    try {
      await cancelInvitation(organizationId, invitationId);
      toast.success("Invitation cancelled");
      await refetchMembers();
    } catch (error) {
      toast.error("Failed to cancel invitation");
    }
  };

  const handleRemoveMember = async (userId: string) => {
    if (!organizationId) return;
    try {
      await removeOrganizationMember(organizationId, userId);
      toast.success("Member removed from organization");
      await refetchMembers();
    } catch (error) {
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
    } catch (error) {
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
          {(error as any).response?.data?.detail || "You don't have permission to manage members or roles."}
        </p>
      </div>
    );
  }

  // Helper to get teams for a user
  const getUserTeams = (userId: string) => {
    if (!teams) return [];
    return teams.filter((t) => t.members.some((m) => m.userId === userId));
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
                                .map((n: string) => n[0])
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

  React.useEffect(() => {
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
