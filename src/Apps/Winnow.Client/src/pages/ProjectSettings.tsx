import { useState, useEffect } from "react";
import { useProject } from "@/hooks/use-project";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { toast } from "sonner";
import { api } from "@/lib/api";
import { useNavigate } from "react-router-dom";
import { PageTitle } from "@/components/ui/page-title";
import { ApiKeyManagement } from "@/components/settings/ApiKeyManagement";
import { GeneralSettings } from "@/components/settings/GeneralSettings";
import { IntegrationsManagement } from "@/components/settings/IntegrationsManagement";
import { ConfirmActionDialog } from "@/components/common/ConfirmActionDialog";

export default function ProjectSettings() {
  const { currentProject, deleteProject } = useProject();
  const [isDeleting, setIsDeleting] = useState(false);
  const [isDeleteConfirmOpen, setIsDeleteConfirmOpen] = useState(false);
  const navigate = useNavigate();

  useEffect(() => {
    const query = new URLSearchParams(window.location.search);
    const verifyIntegration = query.get("verifyIntegration");
    const token = query.get("token");

    if (currentProject && verifyIntegration && token) {
      const verify = async () => {
        try {
          // Clean the URL to avoid double verification
          window.history.replaceState({}, document.title, window.location.pathname);
          
          await api.post(`/projects/${currentProject.id}/integrations/${verifyIntegration}/verify?token=${encodeURIComponent(token)}`);
          toast.success("Email verification successful! The integration is now active.");
        } catch (err: unknown) {
          const message = err instanceof Error ? err.message : String(err);
          toast.error("Email verification failed: " + message);
        }
      };
      verify();
    }
  }, [currentProject]);

  const handleDeleteProject = async () => {
    if (!currentProject) return;

    setIsDeleting(true);
    try {
      await deleteProject(currentProject.id);
      toast.success("Project deleted successfully");
      navigate("/"); // Redirect to dashboard to prevent accidental double-deletes
    } catch {
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
      <div className="mb-8 px-2">
        <PageTitle>Project Configuration</PageTitle>
        <p className="text-muted-foreground mt-1">
          Manage settings and access for {currentProject.name}
        </p>
      </div>

      <div className="space-y-8">
        {/* General & Notifications Section */}
        <GeneralSettings />

        {/* API Key Section */}
        <Card className="flex flex-col">
          <CardHeader>
            <CardTitle>API Key</CardTitle>
            <CardDescription>
              Your project's API Key is used to authenticate the SDK to send
              error reports. For security reasons, it is hashed on our servers
              and cannot be viewed once it's created. If you lose it, you must
              regenerate it.
            </CardDescription>
          </CardHeader>
          <ApiKeyManagement />
        </Card>

        {/* Integrations Section */}
        <IntegrationsManagement />

        {/* Danger Zone */}
        <Card className="border-red-500/20 bg-red-500/5 flex flex-col">
          <CardHeader>
            <CardTitle className="text-red-600 dark:text-red-400">
              Danger Zone
            </CardTitle>
            <CardDescription>
              Irreversible actions regarding this project. Proceed with extreme
              caution.
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="flex flex-col space-y-2">
              <h4 className="text-sm font-medium leading-none">
                Delete Project
              </h4>
              <p className="text-sm text-muted-foreground">
                Permanently delete this project, all of its recorded events, and
                API keys. This action cannot be undone.
              </p>
              <div className="pt-2">
                <Button 
                  variant="destructive" 
                  onClick={() => setIsDeleteConfirmOpen(true)}
                  disabled={isDeleting}
                >
                  {isDeleting ? "Deleting..." : "Delete Project"}
                </Button>
              </div>
            </div>
          </CardContent>
        </Card>
      </div>

      <ConfirmActionDialog
        open={isDeleteConfirmOpen}
        onOpenChange={setIsDeleteConfirmOpen}
        title="Delete Project?"
        description={`Are you absolutely sure you want to delete ${currentProject.name}? This will permanently erase all reports, API keys, and collected data associated with this project. This action is irreversible.`}
        onConfirm={handleDeleteProject}
        actionText="Yes, delete everything"
        variant="destructive"
      />
    </div>
  );
}
