import React, { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '@/lib/api';
import { useProject } from '@/context/ProjectContext';
import { Card, CardContent, CardDescription, CardHeader, CardTitle, CardFooter } from "@/components/ui/card";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Plus, Trash2, Save, Github, Trello, FolderGit2, Edit } from 'lucide-react';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle, DialogTrigger } from "@/components/ui/dialog";
import { toast } from "sonner";

interface IntegrationConfig {
    id: string;
    provider: string;
    // In a real app we wouldn't return full settings to list, but for now we might need them or just parse them safely
    // Actually the list endpoint returns: { id, provider, name }
    // We might need a way to get details or just use the upsert to overwrite.
    // For simplicity, let's just assume we can overwrite.
    name: string;
}

export default function Settings() {
    const [isCheckingOut, setIsCheckingOut] = useState<string | null>(null);
    const [isManaging, setIsManaging] = useState(false);

    const { data: organization, isLoading: isOrgLoading } = useQuery<{ id: string, name: string, subscriptionTier: string }>({
        queryKey: ['current-organization'],
        queryFn: async () => {
            const { data } = await api.get('/organizations/current');
            return data;
        }
    });

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
            await handleManageSubscription();
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

    const handleManageSubscription = async () => {
        setIsManaging(true);
        try {
            const { data } = await api.post('/billing/portal');
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
        <div className="flex flex-col gap-6">
            <h1 className="text-3xl font-bold tracking-tight">Settings</h1>

            <Tabs defaultValue="integrations" className="w-full">
                <TabsList className="grid w-full grid-cols-4 max-w-[500px]">
                    <TabsTrigger value="general">General</TabsTrigger>
                    <TabsTrigger value="billing">Billing</TabsTrigger>
                    <TabsTrigger value="integrations">Integrations</TabsTrigger>
                    <TabsTrigger value="ai">AI Models</TabsTrigger>
                </TabsList>

                <TabsContent value="general" className="mt-6">
                    <Card>
                        <CardHeader>
                            <CardTitle>General Settings</CardTitle>
                            <CardDescription>Manage your workspace preferences.</CardDescription>
                        </CardHeader>
                        <CardContent className="space-y-4">
                            <div className="flex flex-col gap-2">
                                <Label>Organization Name</Label>
                                <Input disabled value={isOrgLoading ? "Loading..." : organization?.name || "Unknown Organization"} readOnly />
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
                                    onClick={handleManageSubscription}
                                    disabled={isManaging}
                                >
                                    {isManaging ? "Redirecting..." : "Manage Subscription / Update Payment Method"}
                                </Button>
                            </CardHeader>
                        </Card>
                    )}

                    <div className="grid gap-6 md:grid-cols-3">
                        <Card>
                            <CardHeader>
                                <CardTitle>Starter</CardTitle>
                                <CardDescription>$15 / month</CardDescription>
                            </CardHeader>
                            <CardContent>
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

                        <Card className="border-primary relative">
                            {!["Pro", "Enterprise"].includes(subscriptionTier) && (
                                <div className="absolute -top-3 left-1/2 -translate-x-1/2 px-3 py-1 bg-primary text-primary-foreground text-xs font-semibold rounded-full">
                                    Recommended
                                </div>
                            )}
                            <CardHeader>
                                <CardTitle>Pro</CardTitle>
                                <CardDescription>$79 / month</CardDescription>
                            </CardHeader>
                            <CardContent>
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

                        <Card>
                            <CardHeader>
                                <CardTitle>Enterprise</CardTitle>
                                <CardDescription>$2,000 / month</CardDescription>
                            </CardHeader>
                            <CardContent>
                                <ul className="list-disc pl-4 space-y-1 text-sm text-muted-foreground">
                                    <li>Dedicated tenant infrastructure</li>
                                    <li>Custom integrations</li>
                                    <li>SLA & Account Manager</li>
                                </ul>
                            </CardContent>
                            <CardFooter>
                                <Button
                                    className="w-full"
                                    variant={subscriptionTier === "Enterprise" ? "secondary" : "outline"}
                                    onClick={() => handleCheckout("Enterprise")}
                                    disabled={isCheckingOut !== null || subscriptionTier === "Enterprise"}
                                >
                                    {getButtonText("Enterprise", subscriptionTier, isCheckingOut)}
                                </Button>
                            </CardFooter>
                        </Card>
                    </div>
                </TabsContent>

                <TabsContent value="integrations" className="mt-6">
                    <IntegrationsSettings />
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

function IntegrationsSettings() {
    const queryClient = useQueryClient();
    const [isAddDialogOpen, setIsAddDialogOpen] = useState(false);
    const [editingId, setEditingId] = useState<string | null>(null);
    const { currentProject } = useProject();

    const { data: integrations, isLoading } = useQuery<IntegrationConfig[]>({
        queryKey: ['integrations', currentProject?.id],
        queryFn: async () => {
            const { data } = await api.get('/integrations');
            return data;
        },
        enabled: !!currentProject,
    });

    const deleteMutation = useMutation({
        mutationFn: async (id: string) => {
            await api.delete(`/integrations/${id}`);
        },
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['integrations'] });
            toast.success("Integration removed");
        }
    });

    return (
        <div className="space-y-6">
            <Card>
                <CardHeader className="flex flex-row items-center justify-between">
                    <div className="space-y-1">
                        <CardTitle>Active Integrations</CardTitle>
                        <CardDescription>Manage your connections to external issue trackers.</CardDescription>
                    </div>
                    <Button onClick={() => { setEditingId(null); setIsAddDialogOpen(true); }}>
                        <Plus className="mr-2 h-4 w-4" /> Add Integration
                    </Button>
                </CardHeader>
                <CardContent>
                    {isLoading ? (
                        <div>Loading...</div>
                    ) : integrations?.length === 0 ? (
                        <div className="text-center py-8 text-muted-foreground">
                            No integrations configured. Add one to get started.
                        </div>
                    ) : (
                        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
                            {integrations?.map((config) => (
                                <Card key={config.id} className="relative overflow-hidden group">
                                    <div className={`absolute top-0 left-0 w-1 h-full ${config.provider.toLowerCase() === 'github' ? 'bg-slate-800' :
                                        config.provider.toLowerCase() === 'trello' ? 'bg-blue-600' :
                                            'bg-blue-500' // Jira
                                        }`} />
                                    <CardHeader className="flex flex-row items-start justify-between space-y-0 pb-2">
                                        <CardTitle className="text-base font-medium flex items-center gap-2">
                                            {config.provider.toLowerCase() === 'github' && <Github className="h-5 w-5" />}
                                            {config.provider.toLowerCase() === 'trello' && <Trello className="h-5 w-5" />}
                                            {config.provider.toLowerCase() === 'jira' && <FolderGit2 className="h-5 w-5" />}
                                            {config.name}
                                        </CardTitle>
                                        <div className="flex gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
                                            <Button
                                                variant="ghost"
                                                size="icon"
                                                className="h-8 w-8 text-muted-foreground hover:text-foreground"
                                                onClick={() => {
                                                    setEditingId(config.id);
                                                    setIsAddDialogOpen(true);
                                                }}
                                            >
                                                <Edit className="h-4 w-4" />
                                            </Button>
                                            <Button
                                                variant="ghost"
                                                size="icon"
                                                className="h-8 w-8 text-red-500 hover:text-red-700 hover:bg-red-50"
                                                onClick={() => {
                                                    if (confirm('Are you sure you want to remove this integration?')) {
                                                        deleteMutation.mutate(config.id);
                                                    }
                                                }}
                                            >
                                                <Trash2 className="h-4 w-4" />
                                            </Button>
                                        </div>
                                    </CardHeader>
                                    <CardContent>
                                        <div className="text-xs text-muted-foreground capitalize">
                                            {config.provider} Provider
                                        </div>
                                    </CardContent>
                                </Card>
                            ))}
                        </div>
                    )}
                </CardContent>
            </Card>

            <AddIntegrationDialog
                open={isAddDialogOpen}
                onOpenChange={setIsAddDialogOpen}
                editId={editingId}
            />
        </div>
    );
}

function AddIntegrationDialog({ open, onOpenChange, editId }: { open: boolean, onOpenChange: (open: boolean) => void, editId: string | null }) {
    const queryClient = useQueryClient();
    const { currentProject } = useProject();
    const [provider, setProvider] = useState<string>("GitHub");
    const [formData, setFormData] = useState<any>({});

    // Fetch details if in edit mode
    useQuery({
        queryKey: ['integration', editId, currentProject?.id],
        queryFn: async () => {
            if (!editId) return null;
            const { data } = await api.get(`/integrations/${editId}`);
            setProvider(data.provider);
            try {
                setFormData(JSON.parse(data.settingsJson));
            } catch (e) { console.error("Failed to parse settings", e); }
            return data;
        },
        enabled: !!editId && open && !!currentProject
    });

    // Reset form when opening in 'add' mode
    React.useEffect(() => {
        if (open && !editId) {
            setProvider("GitHub");
            setFormData({});
        }
    }, [open, editId]);

    const saveMutation = useMutation({
        mutationFn: async (data: any) => {
            await api.post('/integrations', {
                id: editId, // Include ID if editing
                provider: provider,
                settingsJson: JSON.stringify(data),
                isActive: true
            });
        },
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['integrations'] });
            toast.success("Integration saved");
            onOpenChange(false);
            setFormData({});
        },
        onError: () => {
            toast.error("Failed to save integration");
        }
    });

    const handleSave = () => {
        saveMutation.mutate(formData);
    };

    return (
        <Dialog open={open} onOpenChange={onOpenChange}>
            <DialogContent className="sm:max-w-[425px]">
                <DialogHeader>
                    <DialogTitle>{editId ? 'Edit Integration' : 'Add Integration'}</DialogTitle>
                    <DialogDescription>
                        {editId ? 'Modify existing connection settings.' : 'Configure a new connection to an external system.'}
                    </DialogDescription>
                </DialogHeader>
                <div className="grid gap-4 py-4">
                    <div className="grid gap-2">
                        <Label>Provider</Label>
                        <Select value={provider} onValueChange={(val) => { setProvider(val); setFormData({}); }} disabled={!!editId}>
                            <SelectTrigger>
                                <SelectValue placeholder="Select provider" />
                            </SelectTrigger>
                            <SelectContent>
                                <SelectItem value="GitHub">GitHub</SelectItem>
                                <SelectItem value="Trello">Trello</SelectItem>
                                <SelectItem value="Jira">Jira</SelectItem>
                            </SelectContent>
                        </Select>
                    </div>

                    {provider === 'GitHub' && (
                        <>
                            <div className="grid gap-2">
                                <Label>Owner (User/Org)</Label>
                                <Input
                                    value={formData.owner || ''}
                                    onChange={e => setFormData({ ...formData, owner: e.target.value })}
                                    placeholder="e.g. microsoft"
                                />
                            </div>
                            <div className="grid gap-2">
                                <Label>Repository</Label>
                                <Input
                                    value={formData.repo || ''}
                                    onChange={e => setFormData({ ...formData, repo: e.target.value })}
                                    placeholder="e.g. vscode"
                                />
                            </div>
                            <div className="grid gap-2">
                                <Label>Personal Access Token</Label>
                                <Input
                                    type="password"
                                    value={formData.apiKey || ''}
                                    onChange={e => setFormData({ ...formData, apiKey: e.target.value })}
                                    placeholder={editId ? "****** (Unchanged)" : "ghp_..."}
                                />
                            </div>
                        </>
                    )}

                    {provider === 'Trello' && (
                        <>
                            <div className="grid gap-2">
                                <Label>API Key</Label>
                                <Input
                                    type="password"
                                    value={formData.apiKey || ''}
                                    onChange={e => setFormData({ ...formData, apiKey: e.target.value })}
                                    placeholder={editId ? "****** (Unchanged)" : ""}
                                />
                            </div>
                            <div className="grid gap-2">
                                <Label>Token</Label>
                                <Input
                                    type="password"
                                    value={formData.token || ''}
                                    onChange={e => setFormData({ ...formData, token: e.target.value })}
                                    placeholder={editId ? "****** (Unchanged)" : ""}
                                />
                            </div>
                            <div className="grid gap-2">
                                <Label>List ID</Label>
                                <Input
                                    value={formData.listId || ''}
                                    onChange={e => setFormData({ ...formData, listId: e.target.value })}
                                    placeholder="Check board URL .json"
                                />
                            </div>
                        </>
                    )}

                    {provider === 'Jira' && (
                        <>
                            <div className="grid gap-2">
                                <Label>Jira Base URL</Label>
                                <Input
                                    value={formData.baseUrl || ''}
                                    onChange={e => setFormData({ ...formData, baseUrl: e.target.value })}
                                    placeholder="https://your-domain.atlassian.net"
                                />
                            </div>
                            <div className="grid gap-2">
                                <Label>User Email</Label>
                                <Input
                                    value={formData.userEmail || ''}
                                    onChange={e => setFormData({ ...formData, userEmail: e.target.value })}
                                    placeholder="user@example.com"
                                />
                            </div>
                            <div className="grid gap-2">
                                <Label>API Token</Label>
                                <Input
                                    type="password"
                                    value={formData.apiToken || ''}
                                    onChange={e => setFormData({ ...formData, apiToken: e.target.value })}
                                    placeholder={editId ? "****** (Unchanged)" : ""}
                                />
                            </div>
                            <div className="grid gap-2">
                                <Label>Project Key</Label>
                                <Input
                                    value={formData.projectKey || ''}
                                    onChange={e => setFormData({ ...formData, projectKey: e.target.value })}
                                    placeholder="PROJ"
                                />
                            </div>
                        </>
                    )}
                </div>
                <DialogFooter>
                    <Button variant="outline" onClick={() => onOpenChange(false)}>Cancel</Button>
                    <Button onClick={handleSave} disabled={saveMutation.isPending}>
                        {saveMutation.isPending ? 'Saving...' : 'Save Configuration'}
                    </Button>
                </DialogFooter>
            </DialogContent>
        </Dialog>
    );
}
