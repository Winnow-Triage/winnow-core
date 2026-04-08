import { useState, useEffect } from "react";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  CardFooter,
} from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Bell } from "lucide-react";
import { toast } from "sonner";
import { api } from "@/lib/api";
import type { Organization } from "@/types";

interface NotificationSettingsProps {
  organization: Organization | undefined;
  refetch: () => Promise<unknown>;
}

export function NotificationSettings({
  organization,
  refetch,
}: NotificationSettingsProps) {
  const [defaultVolumeThreshold, setDefaultVolumeThreshold] = useState("");
  const [defaultCriticalityThreshold, setDefaultCriticalityThreshold] =
    useState("");
  const [isSavingOrg, setIsSavingOrg] = useState(false);

  useEffect(() => {
    if (organization?.notifications) {
      setDefaultVolumeThreshold(
        organization.notifications.volumeThreshold?.toString() || "",
      );
      setDefaultCriticalityThreshold(
        organization.notifications.criticalityThreshold?.toString() || "",
      );
    }
  }, [organization]);

  const handleSaveNotificationDefaults = async () => {
    setIsSavingOrg(true);
    try {
      await api.put("/organizations/current", {
        name: organization?.name,
        notifications: {
          volumeThreshold: defaultVolumeThreshold
            ? parseInt(defaultVolumeThreshold)
            : null,
          criticalityThreshold: defaultCriticalityThreshold
            ? parseInt(defaultCriticalityThreshold)
            : null,
        },
      });
      await refetch();
      toast.success("Default notification settings updated");
    } catch {
      toast.error("Failed to update default notification settings");
    } finally {
      setIsSavingOrg(false);
    }
  };

  return (
    <Card>
      <CardHeader>
        <div className="flex items-center gap-2">
          <Bell className="h-5 w-5 text-blue-500" />
          <CardTitle>Workspace Notification Defaults</CardTitle>
        </div>
        <CardDescription>
          These thresholds will be used for all projects unless overridden in
          the individual project settings.
        </CardDescription>
      </CardHeader>
      <CardContent className="space-y-6">
        <div className="grid gap-6 md:grid-cols-2 max-w-xl">
          <div className="space-y-2">
            <Label htmlFor="default-volume">Default Volume Threshold</Label>
            <Input
              id="default-volume"
              type="number"
              min="1"
              value={defaultVolumeThreshold}
              onChange={(e) => setDefaultVolumeThreshold(e.target.value)}
              placeholder="e.g. 10"
            />
            <p className="text-xs text-muted-foreground">
              New clusters will trigger an alert when they reach this many
              reports.
            </p>
          </div>

          <div className="space-y-2">
            <Label htmlFor="default-criticality">
              Default Criticality Threshold
            </Label>
            <Input
              id="default-criticality"
              type="number"
              min="1"
              max="10"
              value={defaultCriticalityThreshold}
              onChange={(e) =>
                setDefaultCriticalityThreshold(e.target.value)
              }
              placeholder="e.g. 8"
            />
            <p className="text-xs text-muted-foreground">
              New clusters will trigger an alert if AI assigns a score ≥ this
              value.
            </p>
          </div>
        </div>
      </CardContent>
      <CardFooter className="flex items-center justify-end p-4 px-6 border-t bg-muted/50 rounded-b-3xl">
        <Button
          onClick={handleSaveNotificationDefaults}
          disabled={
            isSavingOrg ||
            (defaultVolumeThreshold ===
              organization?.notifications?.volumeThreshold?.toString() &&
              defaultCriticalityThreshold ===
                organization?.notifications?.criticalityThreshold?.toString())
          }
        >
          {isSavingOrg ? "Saving..." : "Save Default Thresholds"}
        </Button>
      </CardFooter>
    </Card>
  );
}
