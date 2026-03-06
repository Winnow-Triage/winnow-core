import { useState } from "react";
import { useQuery, useQueryClient, useMutation } from "@tanstack/react-query";
import ReactMarkdown from "react-markdown";
import { useParams, Link, useNavigate } from "react-router-dom";
import { api } from "@/lib/api";
import { formatTimeAgo } from "@/lib/utils";
import { toast } from "sonner";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
  ArrowLeft,
  Clock,
  Sparkles,
  LayoutDashboard,
  RotateCw,
  Trash2,
  MoreHorizontal,
} from "lucide-react";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog";

interface ClusterMember {
  id: string;
  title: string;
  message: string;
  status: string;
  createdAt: string;
  confidenceScore?: number;
}

interface ClusterDetailData {
  id: string;
  projectId: string;
  title?: string;
  summary?: string;
  criticalityScore?: number;
  criticalityReasoning?: string;
  status: string;
  createdAt: string;
  reportCount: number;
  firstSeen?: string;
  lastSeen?: string;
  assignedTo?: string;
  velocity1h: number;
  velocity24h: number;
  reports: ClusterMember[];
}

export default function ClusterDetail() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [isGeneratingSummary, setIsGeneratingSummary] = useState(false);
  const [isClearingSummary, setIsClearingSummary] = useState(false);
  const [isUpgradeModalOpen, setIsUpgradeModalOpen] = useState(false);
  const [confirmAction, setConfirmAction] = useState<{
    isOpen: boolean;
    title: string;
    description: string;
    action: () => Promise<void>;
  }>({ isOpen: false, title: "", description: "", action: async () => { } });

  const {
    data: cluster,
    isLoading,
    error,
  } = useQuery<ClusterDetailData>({
    queryKey: ["cluster", id],
    queryFn: async () => {
      const { data } = await api.get(`/clusters/${id}`);
      return data;
    },
    enabled: !!id,
  });

  if (isLoading)
    return (
      <div className="flex flex-col items-center justify-center p-20 gap-4">
        <RotateCw className="h-10 w-10 animate-spin text-blue-500" />
        <p className="text-muted-foreground animate-pulse">
          Refining cluster insights...
        </p>
      </div>
    );

  if (error || !cluster)
    return (
      <div className="flex flex-col items-center justify-center p-20 gap-4">
        <Trash2 className="h-10 w-10 text-red-500" />
        <p className="text-red-500 font-semibold">Error loading cluster.</p>
        <Button variant="outline" onClick={() => navigate(-1)}>
          Go Back
        </Button>
      </div>
    );

  const handleGenerateSummary = async () => {
    setIsGeneratingSummary(true);
    try {
      await api.post(`/reports/${cluster.reports[0].id}/generate-summary`, {});
      await queryClient.invalidateQueries({ queryKey: ["cluster", id] });
      await queryClient.invalidateQueries({ queryKey: ["billing-status"] });
    } catch (e: any) {
      console.error("Failed to generate summary", e);
      if (e.response?.status === 402 || e.response?.status === 403) {
        setIsUpgradeModalOpen(true);
      } else {
        toast.error("Failed to generate summary");
      }
    } finally {
      setIsGeneratingSummary(false);
    }
  };

  const handleClearSummary = async () => {
    setIsClearingSummary(true);
    try {
      await api.post(`/reports/${cluster.reports[0].id}/clear-summary`, {});
      await queryClient.invalidateQueries({ queryKey: ["cluster", id] });
    } catch (e) {
      console.error("Failed to clear summary", e);
    } finally {
      setIsClearingSummary(false);
    }
  };

  const getCriticalityStyles = (score: number | null) => {
    if (!score)
      return {
        color: "text-blue-500",
        bg: "bg-blue-500/10",
        border: "border-blue-500/20",
      };
    if (score >= 8)
      return {
        color: "text-red-500",
        bg: "bg-red-500/10",
        border: "border-red-500/20",
      };
    if (score >= 5)
      return {
        color: "text-amber-500",
        bg: "bg-amber-500/10",
        border: "border-amber-500/20",
      };
    return {
      color: "text-emerald-500",
      bg: "bg-emerald-500/10",
      border: "border-emerald-500/20",
    };
  };

  const criticality = getCriticalityStyles(cluster.criticalityScore ?? null);

  return (
    <div className="flex flex-col gap-8 max-w-6xl mx-auto w-full pb-20">
      {/* Premium Header/Hero Section */}
      <div className="relative group">
        <div className="absolute -inset-1 bg-gradient-to-r from-blue-600 to-purple-600 rounded-2xl blur opacity-10 group-hover:opacity-20 transition duration-1000"></div>
        <div className="relative flex flex-col md:flex-row md:items-end justify-between gap-6 bg-background/60 backdrop-blur-xl border border-white/10 p-8 rounded-2xl shadow-2xl">
          <div className="flex flex-col gap-4">
            <div className="flex items-center gap-3">
              <Button
                variant="ghost"
                size="icon"
                className="hover:bg-white/10 rounded-full"
                onClick={() => navigate(-1)}
              >
                <ArrowLeft className="h-5 w-5" />
              </Button>
              <div className="flex items-center gap-2">
                <Badge
                  variant="outline"
                  className="px-3 py-1 bg-white/5 border-white/10 text-xs font-mono uppercase tracking-widest opacity-70"
                >
                  Cluster Detail
                </Badge>
                <Badge
                  variant={
                    cluster.status === "Closed"
                      ? "success"
                      : cluster.status === "Open"
                        ? "neutral"
                        : "default"
                  }
                  className="rounded-full px-4 shadow-sm"
                >
                  {cluster.status}
                </Badge>
              </div>
            </div>
            <h1 className="text-4xl md:text-5xl font-black tracking-tighter bg-clip-text text-transparent bg-gradient-to-br from-foreground to-foreground/60 leading-tight pb-2 px-2">
              {cluster.title || "Untitled Cluster"}
            </h1>
            <div className="flex flex-wrap items-center gap-6 mt-2">
              <div className="flex items-center gap-2 text-muted-foreground bg-white/5 px-3 py-1.5 rounded-full border border-white/5 shadow-inner">
                <Clock className="h-4 w-4 text-blue-400" />
                <span className="text-sm font-medium">Timeline:</span>
                <span className="text-sm text-foreground">
                  {cluster.firstSeen
                    ? new Date(cluster.firstSeen).toLocaleDateString()
                    : "—"}
                  <span className="mx-2 opacity-30">→</span>
                  {cluster.lastSeen
                    ? new Date(cluster.lastSeen).toLocaleDateString()
                    : "—"}
                </span>
              </div>
              <div className="flex items-center gap-2 text-muted-foreground bg-white/5 px-3 py-1.5 rounded-full border border-white/5 shadow-inner">
                <LayoutDashboard className="h-4 w-4 text-purple-400" />
                <span className="text-sm font-medium text-foreground">
                  {cluster.reportCount}
                </span>
                <span className="text-sm">Reports Impacted</span>
                {cluster.velocity24h > 0 && (
                  <span className="text-xs opacity-50 border-l border-white/10 pl-2">
                    {cluster.velocity24h} in 24h
                  </span>
                )}
              </div>
              {/* 1h velocity — only shown when urgently active */}
              {cluster.velocity1h > 0 && (
                <div className="flex items-center gap-2 text-orange-500 bg-orange-500/10 px-3 py-1.5 rounded-full border border-orange-500/20 shadow-inner animate-pulse">
                  <span className="text-sm font-bold">
                    ⚠️ {cluster.velocity1h} in last hour
                  </span>
                </div>
              )}
            </div>
          </div>

          <div className="flex items-center gap-4">
            <div className="flex flex-col gap-1.5">
              <label className="text-[10px] font-black uppercase tracking-widest opacity-50 ml-1">
                Assigned To
              </label>
              <div className="flex items-center gap-2">
                <input
                  type="text"
                  placeholder="Unassigned"
                  className="bg-white/5 border border-white/10 rounded-xl px-4 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500/50 w-40 transition-all"
                  defaultValue={cluster.assignedTo || ""}
                  onBlur={async (e) => {
                    if (e.target.value !== cluster.assignedTo) {
                      await api.post(`/clusters/${cluster.id}/assign`, {
                        assignedTo: e.target.value,
                      });
                      queryClient.invalidateQueries({
                        queryKey: ["cluster", id],
                      });
                    }
                  }}
                />
              </div>
            </div>

            <ClusterExportMenu
              clusterId={cluster.id}
              projectId={cluster.projectId}
              onExport={() =>
                queryClient.invalidateQueries({ queryKey: ["cluster", id] })
              }
            />

            <Button
              variant="outline"
              className="rounded-xl px-6 h-12 shadow-lg hover:shadow-xl transition-all border-white/10 bg-white/5 backdrop-blur-md self-end"
              onClick={() => {
                setConfirmAction({
                  isOpen: true,
                  title: "Resolve Cluster?",
                  description: `This will mark all ${cluster.reportCount} reports in this cluster as Closed. This action cannot be undone.`,
                  action: async () => {
                    await api.post(
                      `/reports/${cluster.reports[0].id}/close-cluster`,
                      {},
                    );
                    await queryClient.invalidateQueries({
                      queryKey: ["cluster", id],
                    });
                  },
                });
              }}
            >
              Close Cluster
            </Button>
          </div>
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
        {/* AI Perspective Section - Main Insight */}
        <div className="lg:col-span-2 flex flex-col gap-8">
          <Card className="border border-transparent dark:border-white/10 shadow-2xl relative overflow-hidden bg-gradient-to-br from-purple-500/5 via-transparent to-transparent dark:bg-white/[0.02] backdrop-blur-sm rounded-3xl group/card min-h-[400px]">
            {/* Advanced Dynamic Loading Overlay - Now covers the whole card */}
            {isGeneratingSummary && (
              <div className="absolute inset-0 z-50 flex flex-col items-center justify-center transition-all rounded-3xl overflow-hidden">
                {/* Glass Morphic Layer - Massive Blur and Deep Darkening */}
                <div className="absolute inset-0 bg-purple-950/90 backdrop-blur-[100px]"></div>

                {/* Scanning Beam */}
                <div className="absolute inset-0 z-10 pointer-events-none">
                  <div className="w-full h-1/2 bg-gradient-to-b from-transparent via-purple-500/20 to-transparent animate-scan"></div>
                </div>

                {/* Content Shimmer */}
                <div className="absolute inset-0 z-0 animate-shimmer"></div>

                <div className="relative z-20 flex flex-col items-center">
                  <div className="relative mb-8">
                    <div className="absolute inset-0 bg-purple-400 blur-3xl opacity-30 animate-pulse"></div>
                    <div className="p-6 bg-background/90 backdrop-blur-3xl rounded-[2.5rem] border border-purple-400/50 shadow-2xl relative">
                      <Sparkles className="h-14 w-14 animate-spin-slow text-purple-500" />
                      <div className="absolute inset-0 flex items-center justify-center">
                        <div className="h-24 w-24 rounded-full border-t-2 border-purple-500/50 animate-spin"></div>
                      </div>
                    </div>
                  </div>

                  <div className="flex flex-col items-center gap-6">
                    <span className="text-xl font-black tracking-[0.5em] text-white uppercase animate-pulse-gentle drop-shadow-[0_0_15px_rgba(168,85,247,0.5)]">
                      Synthesizing Data
                    </span>
                    <div className="flex gap-3">
                      <div className="h-1.5 w-12 bg-purple-500/30 rounded-full overflow-hidden">
                        <div className="h-full bg-white animate-[shimmer_2s_infinite]"></div>
                      </div>
                      <div className="h-1.5 w-12 bg-purple-500/30 rounded-full overflow-hidden">
                        <div className="h-full bg-white animate-[shimmer_2s_infinite_300ms]"></div>
                      </div>
                      <div className="h-1.5 w-12 bg-purple-500/30 rounded-full overflow-hidden">
                        <div className="h-full bg-white animate-[shimmer_2s_infinite_600ms]"></div>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            )}

            <div className="absolute top-0 right-0 p-8 opacity-[0.03] group-hover/card:opacity-[0.07] transition-opacity duration-1000 transform scale-150 rotate-12 pointer-events-none">
              <Sparkles className="h-64 w-64" />
            </div>

            <CardHeader className="flex flex-row items-start justify-between p-8 pb-4 relative z-10">
              <div className="flex flex-col gap-3">
                <CardTitle className="text-2xl font-bold flex items-center gap-3 tracking-tight">
                  <div className="p-2 bg-purple-500/10 rounded-xl border border-purple-500/20 shadow-lg shadow-purple-500/5">
                    <Sparkles className="h-6 w-6 text-purple-500" />
                  </div>
                  AI Insight
                </CardTitle>
                {cluster.criticalityScore && (
                  <div
                    className={`inline-flex items-center gap-2 px-4 py-1.5 rounded-full border ${criticality.border} ${criticality.bg} shadow-inner`}
                  >
                    <div
                      className={`h-2 w-2 rounded-full animate-pulse ${criticality.color.replace("text", "bg")}`}
                    />
                    <span
                      className={`text-sm font-bold tracking-tight ${criticality.color}`}
                    >
                      Criticality: {cluster.criticalityScore}/10
                    </span>
                  </div>
                )}
              </div>

              <div className="flex items-center gap-2">
                <DropdownMenu>
                  <DropdownMenuTrigger asChild>
                    <Button
                      variant="ghost"
                      size="icon"
                      className="hover:bg-white/10 rounded-full h-10 w-10"
                    >
                      <MoreHorizontal className="h-5 w-5 opacity-60" />
                    </Button>
                  </DropdownMenuTrigger>
                  <DropdownMenuContent
                    align="end"
                    className="rounded-xl border-white/10 backdrop-blur-xl bg-background/90 shadow-2xl"
                  >
                    <DropdownMenuItem
                      disabled={isGeneratingSummary}
                      onClick={handleGenerateSummary}
                      className="p-3"
                    >
                      <RotateCw
                        className={`mr-2 h-4 w-4 ${isGeneratingSummary ? "animate-spin" : ""}`}
                      />
                      Regenerate Analysis
                    </DropdownMenuItem>
                    <DropdownMenuSeparator className="bg-white/10" />
                    <DropdownMenuItem
                      className="text-red-600 p-3"
                      disabled={isClearingSummary}
                      onClick={handleClearSummary}
                    >
                      <Trash2 className="mr-2 h-4 w-4" />
                      Clear History
                    </DropdownMenuItem>
                  </DropdownMenuContent>
                </DropdownMenu>
              </div>
            </CardHeader>

            <CardContent className="relative z-10 p-8 pt-0">
              {cluster.summary ? (
                <div className="space-y-6">
                  <div className="prose prose-lg dark:prose-invert max-w-none leading-relaxed text-foreground/80 font-medium">
                    <ReactMarkdown
                      components={{
                        h3: ({ node, ...props }) => (
                          <h3
                            className="text-xl font-bold mt-6 mb-3 text-foreground"
                            {...props}
                          />
                        ),
                        p: ({ node, ...props }) => (
                          <p className="mb-4 last:mb-0" {...props} />
                        ),
                        strong: ({ node, ...props }) => (
                          <strong
                            className="text-foreground font-black bg-purple-500/5 px-1 rounded"
                            {...props}
                          />
                        ),
                        ul: ({ node, ...props }) => (
                          <ul
                            className="space-y-2 list-disc list-inside marker:text-purple-500"
                            {...props}
                          />
                        ),
                      }}
                    >
                      {cluster.summary}
                    </ReactMarkdown>
                  </div>

                  {cluster.criticalityReasoning && (
                    <Card className="bg-white/5 dark:bg-white/[0.03] border-white/5 dark:border-white/10 border-l-4 border-l-purple-500 p-6 rounded-2xl shadow-inner">
                      <p className="text-sm italic leading-relaxed text-muted-foreground font-medium">
                        <span className="text-2xl text-purple-500 font-serif leading-none mr-1">
                          “
                        </span>
                        {cluster.criticalityReasoning}
                        <span className="text-2xl text-purple-500 font-serif leading-none ml-1">
                          ”
                        </span>
                      </p>
                    </Card>
                  )}
                </div>
              ) : (
                <div className="flex flex-col items-center justify-center py-20 text-center space-y-4">
                  <div className="relative">
                    <Sparkles className="h-16 w-16 opacity-10 animate-pulse" />
                    <div className="absolute inset-0 bg-purple-500 blur-3xl opacity-5 rounded-full"></div>
                  </div>
                  <div className="space-y-2">
                    <p className="text-xl font-bold tracking-tight text-muted-foreground">
                      No insight generated yet.
                    </p>
                    <Button
                      variant="outline"
                      className="rounded-full shadow-sm"
                      onClick={handleGenerateSummary}
                      disabled={isGeneratingSummary}
                    >
                      <RotateCw className="mr-2 h-4 w-4" /> Start AI Analysis
                    </Button>
                  </div>
                </div>
              )}
            </CardContent>
          </Card>
        </div>

        {/* Impact List - Siderbar style */}
        <div className="flex flex-col gap-8">
          <Card className="border border-white/5 dark:border-white/10 shadow-2xl bg-white/5 dark:bg-white/[0.02] backdrop-blur-sm rounded-3xl overflow-hidden">
            <CardHeader className="p-6 pb-2">
              <CardTitle className="text-xl font-bold flex items-center gap-3 tracking-tight">
                <div className="p-2 bg-blue-500/10 rounded-xl border border-blue-500/20 shadow-lg shadow-blue-500/5">
                  <Clock className="h-5 w-5 text-blue-500" />
                </div>
                Impacted Reports
                <Badge
                  variant="secondary"
                  className="ml-auto rounded-full px-3"
                >
                  {cluster.reports.length}
                </Badge>
              </CardTitle>
            </CardHeader>
            <CardContent className="p-4">
              <div className="flex flex-col gap-3 overflow-y-auto max-h-[520px] pr-1">
                {cluster.reports.map((report) => (
                  <Link
                    key={report.id}
                    to={`/reports/${report.id}`}
                    className="group shrink-0 p-4 bg-white/5 border border-white/5 hover:border-blue-500/30 hover:bg-white/10 rounded-2xl transition-all duration-300 shadow-sm relative overflow-hidden"
                  >
                    {/* Confidence Progress Bar */}
                    {report.confidenceScore && (
                      <div
                        className="absolute bottom-0 left-0 h-[2px] bg-blue-500 transition-all group-hover:h-1"
                        style={{
                          width: `${report.confidenceScore * 100}%`,
                          opacity: 0.5,
                        }}
                      />
                    )}

                    <div className="flex flex-col gap-2 relative z-10">
                      <div className="flex items-start justify-between gap-2">
                        <span className="font-bold text-sm tracking-tight group-hover:text-blue-400 transition-colors line-clamp-2">
                          {report.title || report.message}
                        </span>
                        <Badge
                          variant="outline"
                          className="text-[9px] h-fit px-1.5 py-0 bg-white/5 border-white/10 opacity-60"
                        >
                          {report.status}
                        </Badge>
                      </div>

                      <div className="flex items-center justify-between mt-1 text-[11px]">
                        <span className="text-muted-foreground opacity-70">
                          {formatTimeAgo(report.createdAt)}
                        </span>
                        {report.confidenceScore && (
                          <span className="text-blue-500 font-black tracking-tighter">
                            {(report.confidenceScore * 100).toFixed(0)}% MATCH
                          </span>
                        )}
                      </div>
                    </div>
                  </Link>
                ))}
              </div>
            </CardContent>
          </Card>
        </div>
      </div>

      <AlertDialog
        open={confirmAction.isOpen}
        onOpenChange={(open) =>
          setConfirmAction((p) => ({ ...p, isOpen: open }))
        }
      >
        <AlertDialogContent className="rounded-2xl border-white/10 backdrop-blur-2xl bg-background/80 shadow-2xl">
          <AlertDialogHeader>
            <AlertDialogTitle className="text-2xl font-black tracking-tight">
              {confirmAction.title}
            </AlertDialogTitle>
            <AlertDialogDescription className="text-foreground/70 leading-relaxed font-medium">
              {confirmAction.description}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter className="mt-6">
            <AlertDialogCancel className="rounded-xl border-white/10 hover:bg-white/5 font-bold transition-all">
              Cancel
            </AlertDialogCancel>
            <AlertDialogAction
              onClick={confirmAction.action}
              className="rounded-xl bg-blue-600 hover:bg-blue-700 shadow-lg shadow-blue-600/20 font-bold transition-all"
            >
              Proceed
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>

      <AlertDialog
        open={isUpgradeModalOpen}
        onOpenChange={setIsUpgradeModalOpen}
      >
        <AlertDialogContent className="rounded-2xl border-white/10 backdrop-blur-2xl bg-background/80 shadow-2xl">
          <AlertDialogHeader>
            <AlertDialogTitle className="text-2xl font-black tracking-tight text-amber-500">
              Upgrade Required
            </AlertDialogTitle>
            <AlertDialogDescription className="text-foreground/70 leading-relaxed font-medium">
              You have reached your AI summary limit for this billing cycle. Please upgrade your plan to continue using AI triage.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter className="mt-6">
            <AlertDialogCancel className="rounded-xl border-white/10 hover:bg-white/5 font-bold transition-all">
              Cancel
            </AlertDialogCancel>
            <AlertDialogAction
              onClick={() => navigate("/settings?tab=billing")}
              className="rounded-xl bg-amber-600 hover:bg-amber-700 shadow-lg shadow-amber-600/20 font-bold transition-all text-white"
            >
              View Plans
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}

function ClusterExportMenu({
  clusterId,
  projectId,
  onExport,
}: {
  clusterId: string;
  projectId: string;
  onExport: () => void;
}) {
  const { data: integrations } = useQuery<
    { id: string; provider: string; name: string }[]
  >({
    queryKey: ["integrations", projectId],
    queryFn: async () => {
      const { data } = await api.get(`/projects/${projectId}/integrations`);
      return data;
    },
    retry: false,
  });

  const exportMutation = useMutation({
    mutationFn: async (configId: string) => {
      const { data } = await api.post(`/clusters/${clusterId}/export`, {
        configId,
      });
      return data;
    },
    onSuccess: (data: any) => {
      toast.success("Cluster exported successfully");
      onExport();
      if (data?.externalUrl) {
        window.open(data.externalUrl, "_blank");
      }
    },
    onError: (error: any) => {
      const displayMsg =
        error.response?.data?.error || error.message || "Unknown error";
      toast.error("Export Failed", { description: displayMsg });
    },
  });

  if (!integrations || integrations.length === 0) {
    return (
      <Button
        variant="outline"
        className="rounded-xl px-6 h-12 shadow-lg transition-all border-white/10 bg-white/5 backdrop-blur-md self-end opacity-50 cursor-not-allowed"
        disabled
        title="No integration configured — add one in Project Settings"
      >
        Export
      </Button>
    );
  }

  if (integrations.length === 1) {
    return (
      <Button
        variant="outline"
        className="rounded-xl px-6 h-12 shadow-lg hover:shadow-xl transition-all border-white/10 bg-white/5 backdrop-blur-md self-end"
        onClick={() => exportMutation.mutate(integrations[0].id)}
        disabled={exportMutation.isPending}
      >
        {exportMutation.isPending
          ? "Exporting..."
          : `Export to ${integrations[0].provider}`}
      </Button>
    );
  }

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button
          variant="outline"
          className="rounded-xl px-6 h-12 shadow-lg hover:shadow-xl transition-all border-white/10 bg-white/5 backdrop-blur-md self-end"
          disabled={exportMutation.isPending}
        >
          {exportMutation.isPending ? "Exporting..." : "Export To..."}
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end">
        {integrations.map((config) => (
          <DropdownMenuItem
            key={config.id}
            onClick={() => exportMutation.mutate(config.id)}
          >
            Export to {config.name}
          </DropdownMenuItem>
        ))}
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
