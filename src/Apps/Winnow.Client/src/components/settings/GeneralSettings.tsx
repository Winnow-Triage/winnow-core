import { useState, useEffect } from "react";
import { Bell } from "lucide-react";
import { useProject } from "@/hooks/use-project";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  CardFooter,
} from "@/components/ui/card";
import { toast } from "sonner";

export function GeneralSettings() {
  const { currentProject, updateProjectSettings } = useProject();
  const [projectName, setProjectName] = useState("");
  const [notificationThreshold, setNotificationThreshold] = useState<number | string>("");
  const [criticalityThreshold, setCriticalityThreshold] = useState<number | string>("");
  const [isSaving, setIsSaving] = useState(false);

  useEffect(() => {
    if (currentProject) {
      setProjectName(currentProject.name);
      setNotificationThreshold(currentProject.notifications?.volumeThreshold ?? "");
      setCriticalityThreshold(currentProject.notifications?.criticalityThreshold ?? "");
    }
  }, [currentProject]);

  const handleSaveGeneral = async () => {
    if (!currentProject || !projectName.trim()) return;

    setIsSaving(true);
    try {
      await updateProjectSettings(currentProject.id, {
        name: projectName.trim(),
        notifications: {
          volumeThreshold: notificationThreshold === "" ? null : Number(notificationThreshold),
          criticalityThreshold: criticalityThreshold === "" ? null : Number(criticalityThreshold),
        }
      });
      toast.success("Project settings updated");
    } catch (error: unknown) {
      const message = error instanceof Error ? error.message : String(error);
      toast.error(message);
    } finally {
      setIsSaving(false);
    }
  };

  if (!currentProject) return null;

  return (
    <div className="space-y-8">
      {/* General Section */}
      <Card>
        <CardHeader>
          <CardTitle>General Settings</CardTitle>
          <CardDescription>
            Manage your project's basic information.
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="flex flex-col gap-2 max-w-sm">
            <Label htmlFor="project-name">Project Name</Label>
            <Input
              id="project-name"
              value={projectName}
              onChange={(e) => setProjectName(e.target.value)}
              placeholder="My Project"
            />
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <div className="flex items-center gap-2">
            <Bell className="h-5 w-5 text-blue-500" />
            <CardTitle>Notifications</CardTitle>
          </div>
          <CardDescription>
            Configure how and when you want to be notified about cluster activity.
            Leave thresholds empty to use organization defaults.
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-6">
          <div className="grid gap-6 md:grid-cols-2 max-w-xl">
            <div className="space-y-2">
              <Label htmlFor="volume-threshold">Volume Threshold</Label>
              <Input
                id="volume-threshold"
                type="number"
                min="1"
                value={notificationThreshold}
                onChange={(e) => setNotificationThreshold(e.target.value)}
                placeholder="e.g. 10 (Organization Default)"
              />
              <p className="text-xs text-muted-foreground">
                Notify when a cluster reaches exactly this many reports.
              </p>
            </div>

            <div className="space-y-2">
              <Label htmlFor="criticality-threshold">Criticality Threshold</Label>
              <Input
                id="criticality-threshold"
                type="number"
                min="1"
                max="10"
                value={criticalityThreshold}
                onChange={(e) => setCriticalityThreshold(e.target.value)}
                placeholder="e.g. 8 (Organization Default)"
              />
              <p className="text-xs text-muted-foreground">
                Notify when AI assigns a score ≥ this value.
              </p>
            </div>
          </div>
        </CardContent>
        <CardFooter className="justify-end border-t bg-muted/20 py-6 px-6">
          <Button
            onClick={handleSaveGeneral}
            disabled={
              isSaving ||
              !projectName.trim() ||
              (projectName.trim() === currentProject.name &&
               notificationThreshold === (currentProject.notifications?.volumeThreshold ?? "") &&
               criticalityThreshold === (currentProject.notifications?.criticalityThreshold ?? ""))
            }
          >
            {isSaving ? "Saving..." : "Save Settings"}
          </Button>
        </CardFooter>
      </Card>
    </div>
  );
}
