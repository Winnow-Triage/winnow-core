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
import { useNavigate } from "react-router-dom";
import { api } from "@/lib/api";
import type { Organization } from "@/types";

interface OrganizationSettingsProps {
  organization: Organization | undefined;
  isLoading: boolean;
  refetch: () => Promise<unknown>;
}

export function OrganizationSettings({
  organization,
  isLoading,
  refetch,
}: OrganizationSettingsProps) {
  const navigate = useNavigate();
  const [orgName, setOrgName] = useState("");
  const [isSavingOrg, setIsSavingOrg] = useState(false);
  const [isDeletingOrg, setIsDeletingOrg] = useState(false);
  const [isDeleteConfirmOpen, setIsDeleteConfirmOpen] = useState(false);

  useEffect(() => {
    if (organization) {
      setOrgName(organization.name);
    }
  }, [organization]);

  const handleSaveOrganization = async () => {
    if (!orgName.trim() || orgName.trim() === organization?.name) return;

    setIsSavingOrg(true);
    try {
      await api.put("/organizations/current", {
        name: orgName.trim(),
        toxicityFilterEnabled: organization?.toxicityFilterEnabled,
        toxicityLimits: organization?.toxicityLimits,
      });
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

  return (
    <>
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
              disabled={isLoading}
              value={isLoading ? "Loading..." : orgName}
              onChange={(e) => setOrgName(e.target.value)}
              placeholder={isLoading ? "" : "My Organization"}
            />
          </div>
        </CardContent>
        <CardFooter className="flex items-center justify-end p-4 px-6 border-t bg-muted/50 rounded-b-3xl mt-4">
          <Button
            onClick={handleSaveOrganization}
            disabled={
              isSavingOrg ||
              isLoading ||
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
    </>
  );
}
