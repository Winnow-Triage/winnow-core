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
import { Label } from "@/components/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog";
import { Badge } from "@/components/ui/badge";
import { Cpu, Plus, Trash2 } from "lucide-react";
import { toast } from "sonner";
import { api } from "@/lib/api";
import type { Organization, BillingStatus, AIProvider } from "@/types";

interface AISettingsProps {
  organization: Organization | undefined;
  billingStatus: BillingStatus | undefined;
  isBillingLoading: boolean;
  refetch: () => Promise<unknown>;
}

export function AISettings({
  organization,
  billingStatus,
  isBillingLoading,
  refetch,
}: AISettingsProps) {
  const [tokenizerId, setTokenizerId] = useState("Default");
  const [summaryId, setSummaryId] = useState("Default");
  const [customProviders, setCustomProviders] = useState<AIProvider[]>([]);
  const [isSavingOrg, setIsSavingOrg] = useState(false);
  const [isAddProviderOpen, setIsAddProviderOpen] = useState(false); // V1.1 Reserved

  useEffect(() => {
    if (organization) {
      if (organization.aiConfig) {
        setTokenizerId(organization.aiConfig.tokenizer || "Default");
        setSummaryId(organization.aiConfig.summaryAgent || "Default");
        setCustomProviders(organization.aiConfig.customProviders || []);
      }
    }
  }, [organization]);

  const handleSaveAISettings = async () => {
    if (!organization) return;
    setIsSavingOrg(true);
    try {
      await api.put("/organizations/current", {
        name: organization.name,
        aiConfig: {
          tokenizer: tokenizerId,
          summaryAgent: summaryId,
          customProviders: customProviders,
        },
      });
      await refetch();
      toast.success("AI configuration updated");
    } catch {
      toast.error("Failed to update AI settings");
    } finally {
      setIsSavingOrg(false);
    }
  };

  const handleRemoveProvider = (providerId: string) => {
    const updated = customProviders.filter((p) => p.providerId !== providerId);
    setCustomProviders(updated);
    if (tokenizerId === providerId) setTokenizerId("Default");
    if (summaryId === providerId) setSummaryId("Default");
  };

  return (
    <div className="flex flex-col gap-6">
      <Card>
        <CardHeader>
          <CardTitle>AI Configuration</CardTitle>
          <CardDescription>
            Configure optimized Winnow AI services or define your own custom
            providers.
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-6">
          <div className="pt-6 border-t mt-4">
            <div className="flex items-center justify-between mb-4">
              <div className="space-y-1">
                <h4 className="text-sm font-semibold flex items-center gap-2">
                  <Cpu className="w-4 h-4 text-blue-500" />
                  Winnow-Provided AI Services
                </h4>
                <p className="text-xs text-muted-foreground">
                  Configure your intelligent triage services. Winnow provides
                  optimized defaults.
                </p>
              </div>
              <Dialog open={isAddProviderOpen} onOpenChange={setIsAddProviderOpen}>
                <DialogTrigger asChild>
                  <Button
                    variant="outline"
                    size="sm"
                    className="h-8 gap-2 opacity-50 cursor-not-allowed"
                  >
                    <Plus className="w-3.5 h-3.5" />
                    Add Custom Provider
                    <Badge
                      variant="outline"
                      className="text-[8px] h-4 px-1 ml-1 bg-blue-500/10 text-blue-500 border-blue-500/20"
                    >
                      SOON
                    </Badge>
                  </Button>
                </DialogTrigger>
                <DialogContent className="sm:max-w-[425px]">
                  <DialogHeader>
                    <DialogTitle>Add Custom AI Provider</DialogTitle>
                    <DialogDescription>
                      Define a custom LLM or Tokenizer service to use within
                      your organization.
                    </DialogDescription>
                  </DialogHeader>
                  {/* Reserved for V1.1 */}
                </DialogContent>
              </Dialog>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              <div className="p-4 rounded-2xl bg-muted/30 border border-border/40 space-y-4">
                <Label className="font-semibold block mb-2">
                  Tokenizer Service
                </Label>
                <div className="space-y-2">
                  <Label className="text-xs text-muted-foreground uppercase tracking-wider">
                    Active Provider
                  </Label>
                  <Select value={tokenizerId} onValueChange={setTokenizerId}>
                    <SelectTrigger className="h-9">
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="Default">
                        Winnow Default (Optimized)
                      </SelectItem>
                      {customProviders
                        .filter((p) => p.type === "Tokenizer")
                        .map((p) => (
                          <SelectItem key={p.providerId} value={p.providerId}>
                            {p.name} ({p.providerId})
                          </SelectItem>
                        ))}
                    </SelectContent>
                  </Select>
                </div>
              </div>

              <div className="p-4 rounded-2xl bg-muted/30 border border-border/40 space-y-4">
                <Label className="font-semibold block mb-2">
                  Summary Agent
                </Label>
                <div className="space-y-2">
                  <Label className="text-xs text-muted-foreground uppercase tracking-wider">
                    Active Agent
                  </Label>
                  <Select value={summaryId} onValueChange={setSummaryId}>
                    <SelectTrigger className="h-9">
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="Default">
                        Winnow Default Agent
                      </SelectItem>
                      {customProviders
                        .filter((p) => p.type === "SummaryAgent")
                        .map((p) => (
                          <SelectItem key={p.providerId} value={p.providerId}>
                            {p.name} ({p.providerId})
                          </SelectItem>
                        ))}
                    </SelectContent>
                  </Select>
                </div>
              </div>
            </div>

            {customProviders.length > 0 && (
              <div className="mt-6 space-y-3 opacity-50 blur-[1px] pointer-events-none">
                <Label className="text-xs text-muted-foreground uppercase tracking-wider">
                  Custom Providers List
                </Label>
                <div className="space-y-2">
                  {customProviders.map((p) => (
                    <div
                      key={p.providerId}
                      className="flex items-center justify-between p-2 px-4 rounded-xl bg-muted/20 border border-border/40 text-sm"
                    >
                      <div className="flex items-center gap-3">
                        <span className="font-medium">{p.name}</span>
                        <span className="text-xs px-2 py-0.5 rounded-full bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-400">
                          {p.type}
                        </span>
                        <code className="text-[10px] text-muted-foreground">
                          {p.providerId}
                        </code>
                      </div>
                      <Button
                        variant="ghost"
                        size="icon"
                        className="h-8 w-8 text-destructive hover:text-destructive hover:bg-destructive/10"
                        onClick={() => handleRemoveProvider(p.providerId)}
                      >
                        <Trash2 className="w-4 h-4" />
                      </Button>
                    </div>
                  ))}
                </div>
              </div>
            )}
          </div>
        </CardContent>
        <CardFooter className="flex items-center justify-end p-4 px-6 border-t bg-muted/50 rounded-b-3xl">
          <Button
            onClick={handleSaveAISettings}
            disabled={
              isSavingOrg ||
              (tokenizerId === organization?.aiConfig?.tokenizer &&
                summaryId === organization?.aiConfig?.summaryAgent &&
                JSON.stringify(customProviders) ===
                  JSON.stringify(organization?.aiConfig?.customProviders))
            }
          >
            {isSavingOrg ? "Saving..." : "Save AI Settings"}
          </Button>
        </CardFooter>
      </Card>

      {!isBillingLoading && billingStatus && (
        <Card>
          <CardHeader>
            <CardTitle>AI Token Consumption</CardTitle>
            <CardDescription>
              Detailed breakdown of your monthly AI resource usage by model.
            </CardDescription>
          </CardHeader>
          <CardContent>
            <div className="flex items-center justify-between mb-4">
              <h4 className="text-sm font-semibold text-muted-foreground uppercase tracking-tight">
                Monthly Totals
              </h4>
              <div className="flex gap-4">
                <div className="text-right">
                  <div className="text-xs text-muted-foreground uppercase">
                    Input
                  </div>
                  <div className="text-sm font-medium">
                    {billingStatus.monthlyInputTokens.toLocaleString()}
                  </div>
                </div>
                <div className="text-right">
                  <div className="text-xs text-muted-foreground uppercase">
                    Output
                  </div>
                  <div className="text-sm font-medium">
                    {billingStatus.monthlyOutputTokens.toLocaleString()}
                  </div>
                </div>
              </div>
            </div>

            {billingStatus.aiUsageBreakdown?.length > 0 ? (
              <div className="space-y-2">
                {billingStatus.aiUsageBreakdown.map((usage, idx) => (
                  <div
                    key={idx}
                    className="bg-muted/30 p-3 rounded-xl border border-border/50 flex items-center justify-between"
                  >
                    <div className="flex items-center gap-3">
                      <div className="w-8 h-8 rounded-full bg-blue-500/10 flex items-center justify-center">
                        <Cpu className="w-4 h-4 text-blue-500" />
                      </div>
                      <div>
                        <div className="text-sm font-medium">
                          {usage.model}
                        </div>
                        <div className="text-[10px] text-muted-foreground uppercase tracking-wider">
                          {usage.provider}
                        </div>
                      </div>
                    </div>
                    <div className="text-right">
                      <div className="text-sm font-semibold">
                        {(
                          usage.inputTokens + usage.outputTokens
                        ).toLocaleString()}{" "}
                        <span className="text-[10px] font-normal text-muted-foreground">
                          tokens
                        </span>
                      </div>
                      <div className="text-[10px] text-muted-foreground">
                        {usage.callCount} calls
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            ) : (
              <div className="text-sm text-muted-foreground italic text-center py-4 bg-muted/20 rounded-xl border border-dashed text-foreground/50">
                No AI token consumption recorded this month.
              </div>
            )}
          </CardContent>
        </Card>
      )}
    </div>
  );
}
