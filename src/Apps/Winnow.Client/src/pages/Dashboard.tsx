import { useQuery } from "@tanstack/react-query";
import { AlertCircle, Loader2 } from "lucide-react";
import { api } from "@/lib/api";
import { useProject } from "@/context/ProjectContext";

import { WinnowGauge } from "@/components/dashboard/WinnowGauge";
import { TriageFunnelChart } from "@/components/dashboard/TriageFunnelChart";
import { HottestClustersList } from "@/components/dashboard/HottestClustersList";
import { PageTitle } from "@/components/ui/page-title";
import { PendingDecisionsCard } from "@/components/dashboard/PendingDecisionsCard";

// DTO types matching backend
interface DashboardMetrics {
  triage: {
    totalReports: number;
    activeClusters: number;
    noiseReductionRatio: number;
    pendingReviews: number;
    estimatedHoursSaved: number;
  };
  trendingClusters: {
    clusterId: string;
    title: string;
    status: string;
    reportCount: number;
    velocity: number;
    isHot: boolean;
  }[];
  volumeHistory: {
    timestamp: string;
    newUniqueCount: number;
    duplicateCount: number;
  }[];
}

export default function Dashboard() {
  const { currentProject } = useProject();

  const { data, isLoading, error } = useQuery<DashboardMetrics>({
    queryKey: ["dashboardMetrics", currentProject?.id],
    queryFn: async () => {
      const { data } = await api.get("/dashboard/metrics");
      return data;
    },
    refetchInterval: 30000, // Refresh every 30s
    enabled: !!currentProject, // Only fetch if we have a project selected
  });

  if (isLoading) {
    return (
      <div className="flex h-full items-center justify-center">
        <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
      </div>
    );
  }

  if (error) {
    return (
      <div className="p-8">
        <div className="bg-destructive/15 text-destructive p-4 rounded-md border border-destructive/20 flex gap-2 items-center">
          <AlertCircle className="h-4 w-4" />
          <div>
            <p className="font-semibold">Error</p>
            <p className="text-sm">
              Failed to load dashboard metrics. {(error as Error).message}
            </p>
          </div>
        </div>
      </div>
    );
  }

  if (!data) return null;

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-col gap-1">
        <PageTitle>Dashboard</PageTitle>
        <p className="text-muted-foreground">
          Real-time triage overview and actionable insights.
        </p>
      </div>

      <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-4">
        {/* Winnow Gauge (Hero Metric) */}
        <div className="col-span-1 md:col-span-2 lg:col-span-1">
          <WinnowGauge
            percent={data.triage.noiseReductionRatio}
            hoursSaved={data.triage.estimatedHoursSaved}
          />
        </div>

        {/* Triage Funnel (Main Chart) */}
        <div className="col-span-1 md:col-span-2 lg:col-span-2">
          <TriageFunnelChart
            data={data.volumeHistory}
            noiseColor="#E5E7EB"
            signalColor="#3B82F6"
          />
        </div>

        {/* Actionable Cards (Right Column) */}
        <div className="col-span-1 space-y-6">
          <PendingDecisionsCard count={data.triage.pendingReviews} />
          <HottestClustersList clusters={data.trendingClusters} />
        </div>
      </div>
    </div>
  );
}
