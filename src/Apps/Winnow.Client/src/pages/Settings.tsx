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
import { useNavigate } from "react-router-dom";


import { useSearchParams } from 'react-router-dom';

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
                <TabsList className="grid w-full grid-cols-3 max-w-[500px]">
                    <TabsTrigger value="general">General</TabsTrigger>
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



