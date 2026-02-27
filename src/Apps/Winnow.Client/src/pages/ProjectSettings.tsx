import { useState, useEffect } from "react";
import { Clock, AlertTriangle, ShieldCheck, Timer } from "lucide-react";
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useProject } from "@/context/ProjectContext";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Copy, Edit, FolderGit2, Github, Plus, Trash2, Trello } from 'lucide-react';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle, DialogTrigger } from "@/components/ui/dialog";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Label } from "@/components/ui/label";
import { Card, CardContent, CardDescription, CardHeader, CardTitle, CardFooter } from "@/components/ui/card";
import { toast } from "sonner";
import { api, rotateProjectApiKey, revokeProjectSecondaryApiKey } from "@/lib/api";
import { useNavigate } from "react-router-dom";
import { formatDistanceToNow, addHours, addDays } from "date-fns";

interface IntegrationConfig {
    id: string;
    provider: string;
    name: string;
}

function ApiKeyManagementSection() {
    const { currentProject, refreshProjects } = useProject();
    const [isRotating, setIsRotating] = useState(false);
    const [isRevoking, setIsRevoking] = useState(false);
    const [newKey, setNewKey] = useState<string | null>(null);
    const [isRotateDialogOpen, setIsRotateDialogOpen] = useState(false);
    const [expiryOption, setExpiryOption] = useState("30d");

    // Reset new key when project changes
    useEffect(() => {
        setNewKey(null);
        setIsRotateDialogOpen(false);
    }, [currentProject?.id]);

    const handleRotate = async () => {
        if (!currentProject) return;

        let expiresAt: string | null = null;
        if (expiryOption === "1h") expiresAt = addHours(new Date(), 1).toISOString();
        else if (expiryOption === "24h") expiresAt = addDays(new Date(), 1).toISOString();
        else if (expiryOption === "7d") expiresAt = addDays(new Date(), 7).toISOString();
        else if (expiryOption === "30d") expiresAt = addDays(new Date(), 30).toISOString();
        else if (expiryOption === "90d") expiresAt = addDays(new Date(), 90).toISOString();
        else if (expiryOption === "0") expiresAt = new Date().toISOString();

        setIsRotating(true);
        try {
            const apiKey = await rotateProjectApiKey(currentProject.id, expiresAt);
            setNewKey(apiKey);
            toast.success("API Key rotated successfully!");
            setIsRotateDialogOpen(false);
            await refreshProjects();
        } catch (error) {
            console.error("Failed to rotate API key:", error);
            toast.error("Failed to rotate API key.");
        } finally {
            setIsRotating(false);
        }
    };

    const handleRevokeSecondary = async () => {
        if (!currentProject) return;

        setIsRevoking(true);
        try {
            await revokeProjectSecondaryApiKey(currentProject.id);
            toast.success("Secondary API Key revoked.");
            await refreshProjects();
        } catch (error) {
            console.error("Failed to revoke secondary key:", error);
            toast.error("Failed to revoke secondary key.");
        } finally {
            setIsRevoking(false);
        }
    };

    const handleCopy = () => {
        if (newKey) {
            navigator.clipboard.writeText(newKey);
            toast.success("Copied to clipboard!");
        }
    };

    return (
        <div className="flex flex-col">
            <div className="p-6 pt-0 space-y-6">
                {/* Secondary Key Active Warning */}
                {currentProject?.hasSecondaryKey && (
                    <div className="p-4 bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-800 rounded-lg flex items-start gap-3">
                        <Timer className="h-5 w-5 text-amber-600 dark:text-amber-400 mt-0.5 shrink-0" />
                        <div className="flex-1 space-y-2">
                            <div className="flex items-center justify-between">
                                <p className="text-sm font-semibold text-amber-900 dark:text-amber-300">
                                    Secondary (Old) Key is currently active
                                </p>
                                {currentProject.secondaryApiKeyExpiresAt && (
                                    <span className="text-xs font-mono bg-amber-100 dark:bg-amber-900/40 px-2 py-0.5 rounded text-amber-700 dark:text-amber-400">
                                        Expires in {formatDistanceToNow(new Date(currentProject.secondaryApiKeyExpiresAt))}
                                    </span>
                                )}
                            </div>
                            <p className="text-xs text-amber-800/80 dark:text-amber-400/80">
                                Your old API key is still accepting reports to prevent downtime. Update your environment variables soon.
                            </p>
                            <Button
                                variant="outline"
                                size="sm"
                                onClick={handleRevokeSecondary}
                                disabled={isRevoking}
                                className="h-7 text-xs border-amber-300 dark:border-amber-800 hover:bg-amber-100 dark:hover:bg-amber-900/40 text-amber-900 dark:text-amber-300"
                            >
                                {isRevoking ? "Revoking..." : "Revoke Immediately"}
                            </Button>
                        </div>
                    </div>
                )}

                {/* New Key Display */}
                {newKey && (
                    <div className="p-4 bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 rounded-lg">
                        <div className="flex items-center gap-2 mb-3">
                            <ShieldCheck className="h-5 w-5 text-green-600 dark:text-green-400" />
                            <p className="text-sm text-green-800 dark:text-green-300 font-semibold">
                                New API Key Produced
                            </p>
                        </div>
                        <p className="text-xs text-green-700 dark:text-green-400 mb-3">
                            Copy this key now. For your security, we hash keys and cannot show them again.
                        </p>
                        <div className="flex items-center gap-2">
                            <Input value={newKey} readOnly className="font-mono bg-white/50 dark:bg-black/20 border-green-300 dark:border-green-800" />
                            <Button
                                onClick={handleCopy}
                                variant="secondary"
                                size="sm"
                                className="bg-white hover:bg-green-50 text-green-700 border border-green-200 dark:bg-green-950 dark:hover:bg-green-900 dark:text-green-100 dark:border-green-800"
                            >
                                <Copy className="h-4 w-4 mr-2" />
                                Copy
                            </Button>
                        </div>
                    </div>
                )}
            </div>

            <div className="flex items-center justify-between p-4 px-6 border-t bg-muted/50 rounded-b-lg mt-auto">
                <div className="text-xs text-muted-foreground mr-4 flex items-center gap-2">
                    <Clock className="h-4 w-4" />
                    Zero-downtime rotation allows your old key to remain valid while you update your apps.
                </div>

                <Dialog open={isRotateDialogOpen} onOpenChange={setIsRotateDialogOpen}>
                    <DialogTrigger asChild>
                        <Button
                            variant="secondary"
                            onClick={() => setIsRotateDialogOpen(true)}
                            disabled={isRotating || !currentProject}
                        >
                            <Plus className="h-4 w-4 mr-2" />
                            Rotate API Key
                        </Button>
                    </DialogTrigger>
                    <DialogContent>
                        <DialogHeader>
                            <DialogTitle>Rotate API Key</DialogTitle>
                            <DialogDescription>
                                This will generate a new primary key. How long should the current key remain valid?
                            </DialogDescription>
                        </DialogHeader>

                        <div className="py-4 space-y-4">
                            <div className="space-y-2">
                                <Label>Overlap Duration</Label>
                                <Select value={expiryOption} onValueChange={setExpiryOption}>
                                    <SelectTrigger>
                                        <SelectValue placeholder="Select duration" />
                                    </SelectTrigger>
                                    <SelectContent>
                                        <SelectItem value="0">Immediately (No overlap)</SelectItem>
                                        <SelectItem value="1h">1 Hour</SelectItem>
                                        <SelectItem value="24h">24 Hours</SelectItem>
                                        <SelectItem value="7d">7 Days</SelectItem>
                                        <SelectItem value="30d">30 Days (Recommended)</SelectItem>
                                        <SelectItem value="90d">90 Days</SelectItem>
                                        <SelectItem value="never">Until Manually Revoked</SelectItem>
                                    </SelectContent>
                                </Select>
                            </div>

                            <div className="p-3 bg-muted rounded-md border flex items-start gap-3">
                                <AlertTriangle className="h-5 w-5 text-amber-500 shrink-0 mt-0.5" />
                                <div className="text-xs space-y-1">
                                    <p className="font-semibold text-foreground">What happens next?</p>
                                    <p className="text-muted-foreground">
                                        1. A new primary key is generated for you to copy.
                                        <br />
                                        2. The old key moves to 'Secondary' status.
                                        <br />
                                        3. Both keys will work for ingestion until the duration expires.
                                    </p>
                                </div>
                            </div>
                        </div>

                        <DialogFooter>
                            <Button variant="outline" onClick={() => setIsRotateDialogOpen(false)} disabled={isRotating}>Cancel</Button>
                            <Button onClick={handleRotate} disabled={isRotating}>
                                {isRotating ? "Rotating..." : "Start Rotation"}
                            </Button>
                        </DialogFooter>
                    </DialogContent>
                </Dialog>
            </div>
        </div>
    );
}

export default function ProjectSettings() {
    const { currentProject, renameProject, deleteProject } = useProject();
    const [projectName, setProjectName] = useState("");
    const [isSaving, setIsSaving] = useState(false);
    const [isDeleting, setIsDeleting] = useState(false);
    const [isDeleteConfirmOpen, setIsDeleteConfirmOpen] = useState(false);
    const navigate = useNavigate();

    useEffect(() => {
        if (currentProject) {
            setProjectName(currentProject.name);
        }
    }, [currentProject]);

    const handleSaveGeneral = async () => {
        if (!currentProject || !projectName.trim()) return;

        setIsSaving(true);
        try {
            await renameProject(currentProject.id, projectName.trim());
            toast.success("Project updated successfully");
        } catch (error) {
            toast.error("Failed to update project");
        } finally {
            setIsSaving(false);
        }
    };

    const handleDeleteProject = async () => {
        if (!currentProject) return;

        setIsDeleting(true);
        try {
            await deleteProject(currentProject.id);
            toast.success("Project deleted successfully");
            navigate("/"); // Redirect to dashboard to prevent accidental double-deletes
        } catch (error) {
            toast.error("Failed to delete project");
        } finally {
            setIsDeleting(false);
            setIsDeleteConfirmOpen(false);
        }
    };

    if (!currentProject) {
        return (
            <div className="flex items-center justify-center p-8 text-muted-foreground">
                Select a project first.
            </div>
        );
    }

    return (
        <div className="max-w-4xl w-full mx-auto py-8">
            <div className="mb-8">
                <h1 className="text-3xl font-bold tracking-tight">Project Configuration</h1>
                <p className="text-muted-foreground">Manage settings and access for {currentProject.name}</p>
            </div>

            <div className="space-y-8">
                {/* General Section */}
                <div className="rounded-lg border bg-card text-card-foreground shadow-sm">
                    <div className="flex flex-col space-y-1.5 p-6">
                        <h3 className="font-semibold leading-none tracking-tight">General</h3>
                        <p className="text-sm text-muted-foreground">Change basic project details here.</p>
                    </div>
                    <div className="p-6 pt-0 space-y-4">
                        <div className="space-y-2 max-w-sm">
                            <label className="text-sm font-medium leading-none peer-disabled:cursor-not-allowed peer-disabled:opacity-70">
                                Project Name
                            </label>
                            <Input
                                value={projectName}
                                onChange={(e) => setProjectName(e.target.value)}
                                placeholder="My Awesome Project"
                            />
                        </div>
                    </div>
                    <div className="flex items-center justify-end p-4 px-6 border-t bg-muted/50 rounded-b-lg">
                        <Button
                            onClick={handleSaveGeneral}
                            disabled={isSaving || !projectName.trim() || projectName.trim() === currentProject.name}
                        >
                            {isSaving ? "Saving..." : "Save Changes"}
                        </Button>
                    </div>
                </div>

                {/* API Key Section */}
                <div className="rounded-lg border bg-card text-card-foreground shadow-sm flex flex-col">
                    <div className="flex flex-col space-y-1.5 p-6">
                        <h3 className="font-semibold leading-none tracking-tight">API Key</h3>
                        <p className="text-sm text-muted-foreground">
                            Your project's API Key is used to authenticate the SDK to send error reports. For security reasons, it is hashed on our servers and cannot be viewed once it's created. If you lose it, you must regenerate it.
                        </p>
                    </div>
                    <ApiKeyManagementSection />
                </div>

                {/* Integrations Section */}
                <div className="rounded-lg border bg-card text-card-foreground shadow-sm flex flex-col">
                    <div className="flex flex-col space-y-1.5 p-6 border-b">
                        <h3 className="font-semibold leading-none tracking-tight">Integrations</h3>
                        <p className="text-sm text-muted-foreground">Manage external issue tracker connections for this project.</p>
                    </div>
                    <div className="p-6">
                        <IntegrationsSettings />
                    </div>
                </div>

                {/* Danger Zone */}
                <div className="rounded-lg border border-red-500/20 bg-red-500/5 text-card-foreground shadow-sm flex flex-col">
                    <div className="flex flex-col space-y-1.5 p-6">
                        <h3 className="font-semibold leading-none tracking-tight text-red-600 dark:text-red-400">Danger Zone</h3>
                        <p className="text-sm text-muted-foreground">
                            Irreversible actions regarding this project. Proceed with extreme caution.
                        </p>
                    </div>
                    <div className="p-6 pt-0 space-y-4">
                        <div className="flex flex-col space-y-2">
                            <h4 className="text-sm font-medium leading-none">Delete Project</h4>
                            <p className="text-sm text-muted-foreground">
                                Permanently delete this project, all of its recorded events, and API keys. This action cannot be undone.
                            </p>
                            <div className="pt-2">
                                <Dialog open={isDeleteConfirmOpen} onOpenChange={setIsDeleteConfirmOpen}>
                                    <DialogTrigger asChild>
                                        <Button variant="destructive">
                                            Delete Project
                                        </Button>
                                    </DialogTrigger>
                                    <DialogContent>
                                        <DialogHeader>
                                            <DialogTitle>Delete Project</DialogTitle>
                                            <DialogDescription>
                                                Are you absolutely sure you want to delete <b>{currentProject.name}</b>?
                                                <br /><br />
                                                This will permanently erase all reports, API keys, and collected data associated with this project. This action is irreversible.
                                            </DialogDescription>
                                        </DialogHeader>
                                        <DialogFooter>
                                            <Button variant="outline" onClick={() => setIsDeleteConfirmOpen(false)}>
                                                Cancel
                                            </Button>
                                            <Button variant="destructive" onClick={handleDeleteProject} disabled={isDeleting}>
                                                {isDeleting ? "Deleting..." : "Yes, delete everything"}
                                            </Button>
                                        </DialogFooter>
                                    </DialogContent>
                                </Dialog>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
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
            if (!currentProject?.id) return [];
            const { data } = await api.get(`/projects/${currentProject.id}/integrations`);
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
            <div className="flex flex-row items-center justify-between">
                <div></div>
                <Button onClick={() => { setEditingId(null); setIsAddDialogOpen(true); }}>
                    <Plus className="mr-2 h-4 w-4" /> Add Integration
                </Button>
            </div>
            {isLoading ? (
                <div>Loading...</div>
            ) : integrations?.length === 0 ? (
                <div className="text-center py-8 text-muted-foreground">
                    No integrations configured. Add one to get started.
                </div>
            ) : (
                <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-2">
                    {integrations?.map((config) => (
                        <Card key={config.id} className="relative overflow-hidden group border">
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
    useEffect(() => {
        if (open && !editId) {
            setProvider("GitHub");
            setFormData({});
        }
    }, [open, editId]);

    const saveMutation = useMutation({
        mutationFn: async (data: any) => {
            if (!currentProject?.id) return;
            await api.post(`/projects/${currentProject.id}/integrations`, {
                id: editId, // Include ID if editing
                projectId: currentProject.id, // Ensure projectId is passed
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
