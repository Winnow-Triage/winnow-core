import { useState, useEffect, useRef } from "react";
import {
  Plus,
  Trash2,
  Mail,
  Edit,
  AlertTriangle,
} from "lucide-react";
import {
  SiDiscord,
  SiGithub,
  SiJira,
} from "react-icons/si";
import {
  FaTrello
} from "react-icons/fa";
import {
  BsMicrosoftTeams
} from "react-icons/bs";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useProject } from "@/hooks/use-project";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Checkbox } from "@/components/ui/checkbox";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Label } from "@/components/ui/label";
import { toast } from "sonner";
import { api } from "@/lib/api";

const SlackIcon = ({ className }: { className?: string }) => (
  <svg viewBox="0 0 122.8 122.8" className={className} fill="none" xmlns="http://www.w3.org/2000/svg">
    <path d="M25.8 77.6c0 7.1-5.8 12.9-12.9 12.9S0 84.7 0 77.6s5.8-12.9 12.9-12.9h12.9v12.9zm6.4 0c0-7.1 5.8-12.9 12.9-12.9s12.9 5.8 12.9 12.9v32.3c0 7.1-5.8 12.9-12.9 12.9s-12.9-5.8-12.9-12.9V77.6z" fill="#E01E5A"/>
    <path d="M45.1 25.8c-7.1 0-12.9-5.8-12.9-12.9S38 0 45.1 0s12.9 5.8 12.9 12.9v12.9H45.1zm0 6.4c7.1 0 12.9 5.8 12.9 12.9s-5.8 12.9-12.9 12.9H12.9C5.8 58.1 0 52.3 0 45.1s5.8-12.9 12.9-12.9h32.2z" fill="#36C5F0"/>
    <path d="M97 45.1c0-7.1 5.8-12.9 12.9-12.9s12.9 5.8 12.9 12.9s-5.8 12.9-12.9 12.9H97V45.1zm-6.4 0c0 7.1-5.8 12.9-12.9 12.9s-12.9-5.8-12.9-12.9V12.9C64.8 5.8 70.6 0 77.7 0s12.9 5.8 12.9 12.9v32.2z" fill="#2EB67D"/>
    <path d="M77.7 97c7.1 0 12.9 5.8 12.9 12.9s-5.8 12.9-12.9 12.9s-12.9-5.8-12.9-12.9V97h12.9zm0-6.4c-7.1 0-12.9-5.8-12.9-12.9s5.8-12.9 12.9-12.9h32.3c7.1 0 12.9 5.8 12.9 12.9s-5.8 12.9-12.9 12.9H77.7z" fill="#ECB22E"/>
  </svg>
);

interface IntegrationConfig {
  id: string;
  provider: string;
  name: string;
  notificationsEnabled: boolean;
  isVerified?: boolean;
}

export function IntegrationsManagement() {
  const [isAddDialogOpen, setIsAddDialogOpen] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);

  return (
    <Card className="flex flex-col">
      <CardHeader className="border-b mb-6 flex flex-row items-center justify-between">
        <div>
          <CardTitle>Integrations</CardTitle>
          <CardDescription>
            Manage external issue tracker connections for this project.
          </CardDescription>
        </div>
        <Button
          onClick={() => {
            setEditingId(null);
            setIsAddDialogOpen(true);
          }}
        >
          <Plus className="mr-2 h-4 w-4" /> Add Integration
        </Button>
      </CardHeader>
      <CardContent>
        <IntegrationsSettings
          onEdit={(id) => {
            setEditingId(id);
            setIsAddDialogOpen(true);
          }}
        />
      </CardContent>
      <AddIntegrationDialog
        open={isAddDialogOpen}
        onOpenChange={setIsAddDialogOpen}
        editId={editingId}
        key={isAddDialogOpen ? (editingId || "new") : "closed"}
      />
    </Card>
  );
}

function IntegrationsSettings({ onEdit }: { onEdit: (id: string) => void }) {
  const queryClient = useQueryClient();
  const { currentProject } = useProject();

  const { data: integrations, isLoading } = useQuery<IntegrationConfig[]>({
    queryKey: ["integrations", currentProject?.id],
    queryFn: async () => {
      if (!currentProject?.id) return [];
      const { data } = await api.get(
        `/projects/${currentProject.id}/integrations`,
      );
      return data;
    },
    enabled: !!currentProject,
  });

  const deleteMutation = useMutation({
    mutationFn: async (id: string) => {
      await api.delete(`/integrations/${id}`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["integrations"] });
      toast.success("Integration removed");
    },
  });

  return (
    <div className="space-y-6">
      {isLoading ? (
        <div className="flex items-center justify-center py-8">
          <p className="text-sm text-muted-foreground animate-pulse">Loading integrations...</p>
        </div>
      ) : integrations?.length === 0 ? (
        <div className="text-center py-8 text-muted-foreground">
          No integrations configured. Add one to get started.
        </div>
      ) : (
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-2">
          {integrations?.map((config) => (
            <Card
              key={config.id}
              className="relative overflow-hidden group border"
            >
              <div
                className={`absolute top-0 left-0 w-1 h-full ${
                  config.provider.toLowerCase() === "github"
                    ? "bg-slate-800"
                    : config.provider.toLowerCase() === "trello"
                      ? "bg-[#0079bf]"
                      : config.provider.toLowerCase() === "jira"
                        ? "bg-[#0052cc]"
                        : config.provider.toLowerCase() === "discord"
                          ? "bg-[#5865f2]"
                          : config.provider.toLowerCase() === "slack"
                            ? "bg-[#4a154b]"
                            : config.provider.toLowerCase() === "teams" || config.provider.toLowerCase() === "microsoftteams"
                              ? "bg-[#464eb8]"
                              : config.provider.toLowerCase() === "email"
                                ? "bg-stone-500"
                                : "bg-blue-500"
                }`}
              />
              <CardHeader className="flex flex-row items-start justify-between space-y-0 pb-2">
                <CardTitle className="text-base font-medium flex items-center gap-2">
                  {config.provider.toLowerCase() === "github" && (
                    <SiGithub className="h-5 w-5" />
                  )}
                  {config.provider.toLowerCase() === "trello" && (
                    <FaTrello className="h-5 w-5 text-[#0079bf]" />
                  )}
                  {config.provider.toLowerCase() === "jira" && (
                    <SiJira className="h-5 w-5 text-[#0052cc]" />
                  )}
                  {config.provider.toLowerCase() === "discord" && (
                    <SiDiscord className="h-5 w-5 text-[#5865f2]" />
                  )}
                  {config.provider.toLowerCase() === "slack" && (
                    <SlackIcon className="h-5 w-5" />
                  )}
                  {(config.provider.toLowerCase() === "teams" || config.provider.toLowerCase() === "microsoftteams") && (
                    <BsMicrosoftTeams className="h-5 w-5 text-[#464eb8]" />
                  )}
                  {config.provider.toLowerCase() === "email" && (
                    <Mail className="h-5 w-5 text-stone-500" />
                  )}
                  {config.name || `${config.provider} Integration`}
                </CardTitle>
                <div className="flex gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
                  <Button
                    variant="ghost"
                    size="icon"
                    className="h-8 w-8 text-muted-foreground hover:text-foreground"
                    onClick={() => onEdit(config.id)}
                  >
                    <Edit className="h-4 w-4" />
                  </Button>
                  <Button
                    variant="ghost"
                    size="icon"
                    aria-label="Delete Integration"
                    className="h-8 w-8 text-red-500 hover:text-red-700 hover:bg-red-50"
                    onClick={() => {
                      if (
                        confirm(
                          "Are you sure you want to remove this integration?",
                        )
                      ) {
                        deleteMutation.mutate(config.id);
                      }
                    }}
                  >
                    <Trash2 className="h-4 w-4" />
                  </Button>
                </div>
              </CardHeader>
              <CardContent>
                <div className="flex flex-col gap-2">
                  <div className="text-xs text-muted-foreground capitalize">
                    {config.provider} Provider
                  </div>
                  {config.provider.toLowerCase() === "email" && config.isVerified === false && (
                    <div className="inline-flex items-center gap-1.5 text-xs bg-amber-500/10 text-amber-600 dark:text-amber-400 px-2.5 py-0.5 rounded-md font-medium w-fit border border-amber-500/20 mt-1">
                      <AlertTriangle className="h-3.5 w-3.5" />
                      Verification Pending
                    </div>
                  )}
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      )}
    </div>
  );
}

function AddIntegrationDialog({
  open,
  onOpenChange,
  editId,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  editId: string | null;
}) {
  const queryClient = useQueryClient();
  const { currentProject } = useProject();
  const [provider, setProvider] = useState<string>("");
  
  interface IntegrationSettings {
    [key: string]: string | number | boolean | undefined;
  }
  
  const [formData, setFormData] = useState<IntegrationSettings>({});
  const [notificationsEnabled, setNotificationsEnabled] = useState<boolean>(true);
  const [integrationName, setIntegrationName] = useState<string>("");
  const [step, setStep] = useState(editId ? 1 : 0); // 0: Selection, 1: Configuration
  const initialSyncRef = useRef<string | null>(null);

  // State is reset by key in parent

  // Fetch details if in edit mode
  const { data: fetchedData } = useQuery({
    queryKey: ["integration", editId, currentProject?.id],
    queryFn: async () => {
      if (!editId || !currentProject?.id) return null;
      const { data } = await api.get(`/projects/${currentProject.id}/integrations/${editId}`);
      return data;
    },
    enabled: !!editId && open && !!currentProject,
  });

  // Safe way to initialize form state from fetched data without cascading renders
  useEffect(() => {
    if (fetchedData && initialSyncRef.current !== fetchedData.id) {
      initialSyncRef.current = fetchedData.id;
      setProvider(fetchedData.provider);
      setNotificationsEnabled(fetchedData.notificationsEnabled);
      setIntegrationName(fetchedData.name);
      setStep(1);
      try {
        setFormData(JSON.parse(fetchedData.settingsJson));
      } catch {
        console.error("Failed to parse settings");
      }
    }
  }, [fetchedData]); // fetches once per editId mount

  const providers = [
    { id: "GitHub", name: "GitHub", icon: SiGithub, color: "bg-slate-800", text: "text-slate-800" },
    { id: "Trello", name: "Trello", icon: FaTrello, color: "bg-[#0079bf]", text: "text-[#0079bf]" },
    { id: "Jira", name: "Jira", icon: SiJira, color: "bg-[#0052cc]", text: "text-[#0052cc]" },
    { id: "Discord", name: "Discord", icon: SiDiscord, color: "bg-[#5865f2]", text: "text-[#5865f2]" },
    { id: "Slack", name: "Slack", icon: SlackIcon, color: "bg-[#4a154b]", text: "text-[#4a154b]" },
    { id: "Teams", name: "MS Teams", icon: BsMicrosoftTeams, color: "bg-[#464eb8]", text: "text-[#464eb8]" },
    { id: "Email", name: "Email", icon: Mail, color: "bg-stone-500", text: "text-stone-500" },
  ];

  const mutation = useMutation({
    mutationFn: async (data: {
      provider: string;
      name: string;
      notificationsEnabled: boolean;
      settingsJson: string;
    }) => {
      if (editId) {
        return await api.put(`/integrations/${editId}`, data);
      } else {
        return await api.post(`/projects/${currentProject!.id}/integrations`, data);
      }
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["integrations"] });
      onOpenChange(false);
      
      if (!editId && provider === "Email") {
        toast.success("Integration created! Please check your email and click the verification link to activate it.", {
          duration: 10000,
        });
      } else {
        toast.success(editId ? "Integration updated" : "Integration added");
      }
    },
    onError: (error: { response?: { data?: { error?: string } } }) => {
      toast.error(error.response?.data?.error || "Failed to save integration");
    },
  });

  const handleSave = () => {
    mutation.mutate({
      provider,
      name: integrationName || `${provider} (${new Date().toLocaleDateString()})`,
      notificationsEnabled,
      settingsJson: JSON.stringify(formData),
    });
  };

  const updateField = (key: string, value: string | boolean) => {
    setFormData((prev) => ({ ...prev, [key]: value }));
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-md">
        <DialogHeader>
          <DialogTitle>
            {editId ? "Edit Integration" : "Add Integration"}
          </DialogTitle>
          <DialogDescription>
            {step === 0
              ? "Choose the service you want to connect to Winnow."
              : `Configure your ${provider} connection settings.`}
          </DialogDescription>
        </DialogHeader>

        {step === 0 ? (
          <div className="grid grid-cols-2 gap-3 py-4">
            {providers.map((p) => (
              <Button
                key={p.id}
                variant="outline"
                className="h-24 flex flex-col gap-2 border-2 hover:border-blue-500 hover:bg-blue-50 transition-all group"
                onClick={() => {
                  setProvider(p.id);
                  setStep(1);
                }}
              >
                <p.icon className={`h-8 w-8 ${p.text} transition-transform group-hover:scale-110`} />
                <span className="font-semibold">{p.name}</span>
              </Button>
            ))}
          </div>
        ) : (
          <div className="space-y-4 py-4">
            <div className="space-y-2">
              <Label>Integration Display Name</Label>
              <Input
                placeholder="e.g. Engineering Slack"
                value={integrationName}
                onChange={(e) => setIntegrationName(e.target.value)}
              />
            </div>

            {provider === "Slack" && (
              <div className="space-y-2">
                <Label>Webhook URL</Label>
                <Input
                  placeholder="https://hooks.slack.com/services/..."
                  value={(formData.webhookUrl as string) || ""}
                  onChange={(e) => updateField("webhookUrl", e.target.value)}
                />
              </div>
            )}

            {provider === "Discord" && (
              <div className="space-y-2">
                <Label>Webhook URL</Label>
                <Input
                  placeholder="https://discord.com/api/webhooks/..."
                  value={(formData.webhookUrl as string) || ""}
                  onChange={(e) => updateField("webhookUrl", e.target.value)}
                />
              </div>
            )}

            {provider === "Teams" && (
              <div className="space-y-2">
                <Label>Webhook URL</Label>
                <Input
                  placeholder="https://outlook.office.com/webhook/..."
                  value={(formData.webhookUrl as string) || ""}
                  onChange={(e) => updateField("webhookUrl", e.target.value)}
                />
              </div>
            )}

            {provider === "Email" && (
              <div className="space-y-2">
                <Label>Destination Email</Label>
                <Input
                  type="email"
                  placeholder="engineering-leads@acme.com"
                  value={(formData.email as string) || ""}
                  onChange={(e) => updateField("email", e.target.value)}
                />
                <p className="text-[10px] text-muted-foreground mt-1">
                  We'll send a verification link to this address before activating.
                </p>
              </div>
            )}

            {(provider === "GitHub" || provider === "Trello" || provider === "Jira") && (
              <div className="p-4 bg-muted border rounded-lg text-sm text-muted-foreground">
                Detailed configuration for {provider} is currently handled via the CLI or environment variables in this version.
              </div>
            )}

            <div className="flex items-center space-x-2 pt-2 border-t mt-4">
              <Checkbox
                id="notify"
                checked={notificationsEnabled}
                onCheckedChange={(checked) =>
                  setNotificationsEnabled(checked === true)
                }
              />
              <Label
                htmlFor="notify"
                className="text-sm font-medium leading-none peer-disabled:cursor-not-allowed peer-disabled:opacity-70 cursor-pointer"
              >
                Enable cluster notifications
              </Label>
            </div>
          </div>
        )}

        <DialogFooter>
          {step === 1 && !editId && (
            <Button variant="ghost" onClick={() => setStep(0)}>
              Back
            </Button>
          )}
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            Cancel
          </Button>
          {step === 1 && (
            <Button onClick={handleSave} disabled={mutation.isPending}>
              {mutation.isPending ? "Saving..." : editId ? "Update" : "Create"}
            </Button>
          )}
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
