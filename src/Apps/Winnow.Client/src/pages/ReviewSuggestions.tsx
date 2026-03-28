import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { api } from "@/lib/api";
import { useProject } from "@/context/ProjectContext";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Loader2, CheckCircle, XCircle, Check, Info } from "lucide-react";
import { toast } from "sonner";
import { useNavigate } from "react-router-dom";
import { cn } from "@/lib/utils";
import { PageTitle } from "@/components/ui/page-title";

interface ReviewItem {
  sourceId: string;
  sourceTitle: string;
  sourceMessage: string;
  sourceStackTrace: string | null;
  sourceAssignedTo: string;
  sourceCreatedAt: string;
  targetId: string;
  targetTitle: string | null;
  targetSummary: string | null;
  confidenceScore: number;
  type: "Report" | "Cluster";
}

export default function ReviewSuggestions() {
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  const { currentProject } = useProject();
  const [currentIndex, setCurrentIndex] = useState(0);

  const { data: queue, isLoading } = useQuery<ReviewItem[]>({
    queryKey: ["reviewQueue", currentProject?.id],
    queryFn: async () => {
      const res = await api.get("/reports/review-queue");
      return res.data;
    },
    enabled: !!currentProject,
  });

  const currentItem = queue && queue.length > 0 ? queue[currentIndex] : null;

  const dismissMutation = useMutation({
    mutationFn: async ({ id, type }: { id: string; type: "Report" | "Cluster" }) => {
      const endpoint = type === "Report"
        ? `/reports/${id}/dismiss-suggestion`
        : `/clusters/${id}/dismiss-merge-suggestion`;
      await api.post(endpoint, {});
    },
    onSuccess: () => {
      handleNext();
    },
    onError: (error: any) => {
      toast.error(error.response?.data?.message || "Failed to dismiss suggestion");
    },
  });

  const acceptMutation = useMutation({
    mutationFn: async ({ id, type }: { id: string; type: "Report" | "Cluster" }) => {
      const endpoint = type === "Report"
        ? `/reports/${id}/accept-suggestion`
        : `/clusters/${id}/accept-merge-suggestion`;
      await api.post(endpoint, {});
    },
    onSuccess: (_, variables) => {
      toast.success(variables.type === "Report" ? "Merged into cluster" : "Clusters merged successfully");
      handleNext();
    },
    onError: (error: any) => {
      toast.error(error.response?.data?.message || "Failed to accept suggestion");
    },
  });

  const handleNext = () => {
    if (!queue) return;

    if (currentIndex < queue.length - 1) {
      setCurrentIndex((prev) => prev + 1);
    } else {
      queryClient.invalidateQueries({ queryKey: ["reviewQueue"] });
      setCurrentIndex(0);
    }
  };

  if (isLoading) {
    return (
      <div className="flex h-[80vh] items-center justify-center">
        <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
      </div>
    );
  }

  if (!queue || queue.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center h-[80vh] gap-4">
        <div className="rounded-full bg-green-100 p-6 dark:bg-green-900/20">
          <CheckCircle className="h-12 w-12 text-green-600 dark:text-green-500" />
        </div>
        <h2 className="text-2xl font-bold tracking-tight">All Caught Up!</h2>
        <p className="text-muted-foreground">
          No more suggestions to review right now.
        </p>
        <Button onClick={() => navigate("/")}>Back to Dashboard</Button>
      </div>
    );
  }

  if (!currentItem) return null;

  return (
    <div className="flex flex-col flex-1 min-h-0">
      <div className="shrink-0 mb-6">
        <div className="flex items-center gap-4">
          <PageTitle>Review Suggestions</PageTitle>
          <Badge variant="secondary" className="font-semibold px-2 py-0.5 bg-primary/10 text-primary border-primary/20">
            {queue.length} Pending
          </Badge>
        </div>
      </div>

      <div className="flex-1 overflow-hidden flex flex-col min-h-0">
        <div className="flex-1 grid grid-cols-1 lg:grid-cols-2 gap-6 min-h-0">
          {/* Left Panel: Source */}
          <Card className="flex flex-col overflow-hidden border-t-4 border-t-blue-500 shadow-sm">
            <CardHeader className="bg-muted/30 border-b shrink-0 py-4">
              <div className="flex items-center justify-between mb-1">
                <Badge className={cn(
                  currentItem.type === "Report" ? "bg-blue-500" : "bg-indigo-500",
                  "text-white"
                )}>
                  {currentItem.type === "Report" ? "New Report" : "Existing Cluster"}
                </Badge>
                <span className="text-xs text-muted-foreground">
                  {new Date(currentItem.sourceCreatedAt).toLocaleDateString()}
                </span>
              </div>
              <CardTitle className="text-lg leading-tight line-clamp-2">
                {currentItem.sourceTitle}
              </CardTitle>
              <div className="flex items-center gap-1.5 text-xs text-muted-foreground mt-1">
                <Info className="w-3 h-3" />
                Assigned to {currentItem.sourceAssignedTo}
              </div>
            </CardHeader>
            <CardContent className="flex-1 overflow-y-auto p-4 md:p-6 custom-scrollbar">
              <div className="space-y-4">
                <div>
                  <h4 className="text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-2">Message</h4>
                  <p className="text-sm leading-relaxed whitespace-pre-wrap">
                    {currentItem.sourceMessage}
                  </p>
                </div>
                {currentItem.sourceStackTrace && (
                  <div>
                    <h4 className="text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-2">Stack Trace</h4>
                    <pre className="p-3 rounded-lg bg-muted font-mono text-xs overflow-x-auto border">
                      {currentItem.sourceStackTrace}
                    </pre>
                  </div>
                )}
              </div>
            </CardContent>
          </Card>

          {/* Right Panel: Target Cluster */}
          <Card className="flex flex-col overflow-hidden border-t-4 border-t-purple-500 shadow-sm relative">
            <div className="absolute top-3 right-3 z-10">
              <Badge
                variant="secondary"
                className={cn(
                  "text-xs font-bold px-2 py-0.5 border shadow-sm",
                  currentItem.confidenceScore > 0.8
                    ? "bg-emerald-100 text-emerald-700 border-emerald-200 dark:bg-emerald-900/30 dark:text-emerald-400"
                    : "bg-amber-100 text-amber-700 border-amber-200 dark:bg-amber-900/30 dark:text-amber-400",
                )}
              >
                Match: {Math.round(currentItem.confidenceScore * 100)}%
              </Badge>
            </div>

            <CardHeader className="bg-purple-50/50 dark:bg-purple-950/10 border-b shrink-0 py-4">
              <div className="flex items-center justify-between mb-1">
                <Badge
                  variant="outline"
                  className="border-purple-200 text-purple-700 dark:border-purple-800 dark:text-purple-400"
                >
                  {currentItem.type === "Report" ? "Suggested Destination" : "Suggested Target Cluster"}
                </Badge>
              </div>
              <CardTitle className="text-lg leading-tight line-clamp-2">
                {currentItem.targetTitle || "Untitled Cluster"}
              </CardTitle>
              <div className="flex items-center gap-1.5 text-xs text-muted-foreground mt-1">
                <Info className="w-3 h-3" />
                Existing Knowledge Base
              </div>
            </CardHeader>
            <CardContent className="flex-1 overflow-y-auto p-4 md:p-6 custom-scrollbar">
              <div className="space-y-4">
                <div>
                  <h4 className="text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-2">Cluster Summary</h4>
                  {currentItem.targetSummary ? (
                    <p className="text-sm leading-relaxed whitespace-pre-wrap">
                      {currentItem.targetSummary}
                    </p>
                  ) : (
                    <p className="text-sm text-muted-foreground italic">
                      No summary available.
                    </p>
                  )}
                </div>
              </div>
            </CardContent>
          </Card>
        </div>

        {/* Action Bar */}
        <div className="shrink-0 pt-6 pb-2 pb-safe">
          <div className="relative bg-card/50 backdrop-blur-md border border-border/50 shadow-lg rounded-2xl p-4 flex items-center justify-center gap-4 min-h-[80px]">
            <div className="absolute left-6 hidden sm:block text-sm text-muted-foreground whitespace-nowrap">
              Item {currentIndex + 1} of {queue.length}
            </div>

            <div className="flex items-center justify-center gap-3 w-full sm:w-auto">
              <Button
                variant="outline"
                size="lg"
                className="flex-1 sm:flex-none sm:w-36 gap-2 border-red-200 hover:bg-red-50 hover:text-red-600 dark:border-red-900/50 dark:hover:bg-red-950/20"
                onClick={() =>
                  dismissMutation.mutate({ id: currentItem.sourceId, type: currentItem.type })
                }
                disabled={dismissMutation.isPending || acceptMutation.isPending}
              >
                <XCircle className="w-4 h-4" />
                Reject
              </Button>

              <Button
                variant="ghost"
                size="lg"
                className="flex-1 sm:flex-none text-muted-foreground"
                onClick={handleNext}
                disabled={dismissMutation.isPending || acceptMutation.isPending}
              >
                Skip
              </Button>

              <Button
                size="lg"
                className="flex-1 sm:flex-none sm:w-44 gap-2 bg-gradient-to-r from-blue-600 to-indigo-600 hover:from-blue-700 hover:to-indigo-700 text-white shadow-md border-0"
                onClick={() => acceptMutation.mutate({ id: currentItem.sourceId, type: currentItem.type })}
                disabled={dismissMutation.isPending || acceptMutation.isPending}
              >
                <Check className="w-4 h-4" />
                Confirm Merge
              </Button>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
