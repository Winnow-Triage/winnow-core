import { useState, useEffect } from "react";
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
import { api } from "@/lib/api";
import { useNavigate } from "react-router-dom";

interface IntegrationConfig {
    id: string;
    provider: string;
    name: string;
}

function RegenerateApiKeySection() {
    const { currentProject } = useProject();
    const [isRegenerating, setIsRegenerating] = useState(false);
    const [newKey, setNewKey] = useState<string | null>(null);
    const [isConfirmOpen, setIsConfirmOpen] = useState(false);

    // Reset new key when project changes
    useEffect(() => {
        setNewKey(null);
        setIsConfirmOpen(false);
    }, [currentProject?.id]);

    const handleRegenerate = async () => {
        if (!currentProject) return;

        setIsRegenerating(true);
        setNewKey(null);

        try {
            const { data } = await api.post(`/projects/${currentProject.id}/api-key/regenerate`);
            if (data?.apiKey) {
                setNewKey(data.apiKey);
                toast.success("API Key regenerated successfully!");
                setIsConfirmOpen(false);
            }
        } catch (error) {
            console.error("Failed to regenerate API key:", error);
            toast.error("Failed to regenerate API key.");
        } finally {
            setIsRegenerating(false);
        }
    };

    const handleCopy = () => {
        if (newKey) {
            navigator.clipboard.writeText(newKey);
            toast.success("Copied to clipboard!");
        }
    };

    if (newKey) {
        return (
            <div className="p-6 pt-0 space-y-4">
                <div className="p-4 bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 rounded-md">
                    <p className="text-sm text-green-800 dark:text-green-300 font-semibold mb-2">
                        New API Key Generated! Copy it now, as you won't be able to see it again.
                    </p>
                    <div className="flex items-center gap-2">
                        <Input value={newKey} readOnly className="font-mono bg-white/50 dark:bg-black/20 border-green-300 dark:border-green-800" />
                        <Button
                            onClick={handleCopy}
                            variant="secondary"
                            className="bg-white hover:bg-green-50 text-green-700 border border-green-200 dark:bg-green-950 dark:hover:bg-green-900 dark:text-green-100 dark:border-green-800"
                        >
                            <Copy className="h-4 w-4 mr-2" />
                            Copy
                        </Button>
                    </div>
                </div>
            </div>
        );
    }

    return (
        <div className="flex items-center justify-between p-4 px-6 border-t bg-muted/50 rounded-b-lg mt-auto">
            <div className="text-sm text-muted-foreground mr-4">
                Warning: Regenerating your project's key will break any current SDK integrations using it until you update them.
            </div>

            <Dialog open={isConfirmOpen} onOpenChange={setIsConfirmOpen}>
                <DialogTrigger asChild>
                    <Button
                        variant="destructive"
                        onClick={() => setIsConfirmOpen(true)}
                        disabled={isRegenerating || !currentProject}
                    >
                        {isRegenerating ? "Regenerating..." : "Regenerate API Key"}
                    </Button>
                </DialogTrigger>
                <DialogContent>
                    <DialogHeader>
                        <DialogTitle>Regenerate API Key</DialogTitle>
                        <DialogDescription>
                            Are you sure you want to regenerate the API key for {currentProject?.name}? The old key will stop working immediately, and you will need to update your SDK initialization code in all connected apps.
                        </DialogDescription>
                    </DialogHeader>
                    <DialogFooter>
                        <Button variant="outline" onClick={() => setIsConfirmOpen(false)} disabled={isRegenerating}>Cancel</Button>
                        <Button variant="destructive" onClick={handleRegenerate} disabled={isRegenerating}>
                            {isRegenerating ? "Regenerating..." : "Yes, Regenerate Key"}
                        </Button>
                    </DialogFooter>
                </DialogContent>
            </Dialog>
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
                    <RegenerateApiKeySection />
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
