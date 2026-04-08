import ReactMarkdown from "react-markdown";
import { Sparkles, RotateCw, MoreHorizontal, Trash2 } from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";

interface AiInsightCardProps {
  cluster: {
    summary?: string;
    criticalityScore?: number;
    criticalityReasoning?: string;
  };
  isGeneratingSummary: boolean;
  isClearingSummary: boolean;
  onGenerateSummary: () => void;
  onClearSummary: () => void;
}

export function AiInsightCard({
  cluster,
  isGeneratingSummary,
  isClearingSummary,
  onGenerateSummary,
  onClearSummary,
}: AiInsightCardProps) {
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
    <Card className="border border-transparent dark:border-white/10 shadow-2xl relative overflow-hidden bg-gradient-to-br from-purple-500/5 via-transparent to-transparent dark:bg-white/[0.02] backdrop-blur-sm rounded-3xl group/card min-h-[400px]">
      {/* Advanced Dynamic Loading Overlay */}
      {isGeneratingSummary && (
        <div className="absolute inset-0 z-50 flex flex-col items-center justify-center transition-all rounded-3xl overflow-hidden">
          <div className="absolute inset-0 bg-purple-950/90 backdrop-blur-[100px]"></div>
          <div className="absolute inset-0 z-10 pointer-events-none">
            <div className="w-full h-1/2 bg-gradient-to-b from-transparent via-purple-500/20 to-transparent animate-scan"></div>
          </div>
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
                onClick={onGenerateSummary}
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
                onClick={onClearSummary}
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
                  h3: ({ children, ...props }) => (
                    <h3
                      className="text-xl font-bold mt-6 mb-3 text-foreground"
                      {...props}
                    >
                      {children}
                    </h3>
                  ),
                  p: ({ ...props }) => (
                    <p className="mb-4 last:mb-0" {...props} />
                  ),
                  strong: ({ ...props }) => (
                    <strong
                      className="text-foreground font-black bg-purple-500/5 px-1 rounded"
                      {...props}
                    />
                  ),
                  ul: ({ ...props }) => (
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
                onClick={onGenerateSummary}
                disabled={isGeneratingSummary}
              >
                <RotateCw className="mr-2 h-4 w-4" /> Start AI Analysis
              </Button>
            </div>
          </div>
        )}
      </CardContent>
    </Card>
  );
}
