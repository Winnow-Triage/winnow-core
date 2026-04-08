import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { ArrowLeft, Clock, LayoutDashboard } from "lucide-react";
import { useNavigate } from "react-router-dom";
import { api } from "@/lib/api";
import { useQueryClient } from "@tanstack/react-query";
import { ClusterExportMenu } from "./ClusterExportMenu";

interface ClusterHeroProps {
  cluster: {
    id: string;
    projectId: string;
    title?: string;
    status: string;
    firstSeen?: string;
    lastSeen?: string;
    reportCount: number;
    velocity1h: number;
    velocity24h: number;
    assignedTo?: string;
  };
  onCloseCluster: () => void;
}

export function ClusterHero({ cluster, onCloseCluster }: ClusterHeroProps) {
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  return (
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
            <label
              htmlFor="assigned-to"
              className="text-[10px] font-black uppercase tracking-widest opacity-50 ml-1"
            >
              Assigned To
            </label>
            <div className="flex items-center gap-2">
              <input
                id="assigned-to"
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
                      queryKey: ["cluster", cluster.id],
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
              queryClient.invalidateQueries({ queryKey: ["cluster", cluster.id] })
            }
          />

          <Button
            variant="outline"
            className="rounded-xl px-6 h-12 shadow-lg hover:shadow-xl transition-all border-white/10 bg-white/5 backdrop-blur-md self-end"
            onClick={onCloseCluster}
          >
            Close Cluster
          </Button>
        </div>
      </div>
    </div>
  );
}
