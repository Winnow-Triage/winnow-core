import { useState, useEffect } from "react";
import { useProject } from "@/context/ProjectContext";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Copy } from 'lucide-react';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle, DialogTrigger } from "@/components/ui/dialog";
import { toast } from "sonner";
import { api } from "@/lib/api";
import { useNavigate } from "react-router-dom";

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
        <div className="max-w-4xl mx-auto py-8">
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
