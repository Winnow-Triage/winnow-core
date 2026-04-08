import { useState, useEffect } from "react";
import {
  Clock,
  AlertTriangle,
  ShieldCheck,
  Timer,
  Plus,
  Copy,
} from "lucide-react";
import { useProject } from "@/hooks/use-project";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Label } from "@/components/ui/label";
import { CardFooter } from "@/components/ui/card";
import { toast } from "sonner";
import {
  rotateProjectApiKey,
  revokeProjectSecondaryApiKey,
} from "@/lib/api";
import { formatDistanceToNow, addHours, addDays } from "date-fns";

export function ApiKeyManagement() {
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
    if (expiryOption === "1h")
      expiresAt = addHours(new Date(), 1).toISOString();
    else if (expiryOption === "24h")
      expiresAt = addDays(new Date(), 1).toISOString();
    else if (expiryOption === "7d")
      expiresAt = addDays(new Date(), 7).toISOString();
    else if (expiryOption === "30d")
      expiresAt = addDays(new Date(), 30).toISOString();
    else if (expiryOption === "90d")
      expiresAt = addDays(new Date(), 90).toISOString();
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
                    Expires in{" "}
                    {formatDistanceToNow(
                      new Date(currentProject.secondaryApiKeyExpiresAt),
                    )}
                  </span>
                )}
              </div>
              <p className="text-xs text-amber-800/80 dark:text-amber-400/80">
                Your old API key is still accepting reports to prevent downtime.
                Update your environment variables soon.
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
              Copy this key now. For your security, we hash keys and cannot show
              them again.
            </p>
            <div className="flex items-center gap-2">
              <Input
                value={newKey}
                readOnly
                className="font-mono bg-white/50 dark:bg-black/20 border-green-300 dark:border-green-800"
              />
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

      <CardFooter className="flex items-center justify-between border-t bg-muted/20 py-6 px-6">
        <div className="text-xs text-muted-foreground mr-4 flex items-center gap-2">
          <Clock className="h-4 w-4" />
          Zero-downtime rotation allows your old key to remain valid while you
          update your apps.
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
                This will generate a new primary key. How long should the
                current key remain valid?
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
                    <SelectItem value="never">
                      Until Manually Revoked
                    </SelectItem>
                  </SelectContent>
                </Select>
              </div>

              <div className="p-3 bg-muted rounded-md border flex items-start gap-3">
                <AlertTriangle className="h-5 w-5 text-amber-500 shrink-0 mt-0.5" />
                <div className="text-xs space-y-1">
                  <p className="font-semibold text-foreground">
                    What happens next?
                  </p>
                  <p className="text-muted-foreground">
                    1. A new primary key is generated for you to copy.
                    <br />
                    2. The old key moves to 'Secondary' status.
                    <br />
                    3. Both keys will work for ingestion until the duration
                    expires.
                  </p>
                </div>
              </div>
            </div>

            <DialogFooter>
              <Button
                variant="outline"
                onClick={() => setIsRotateDialogOpen(false)}
                disabled={isRotating}
              >
                Cancel
              </Button>
              <Button onClick={handleRotate} disabled={isRotating}>
                {isRotating ? "Rotating..." : "Start Rotation"}
              </Button>
            </DialogFooter>
          </DialogContent>
        </Dialog>
      </CardFooter>
    </div>
  );
}
