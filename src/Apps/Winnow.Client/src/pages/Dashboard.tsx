import { useQuery } from "@tanstack/react-query";
import { api } from "@/lib/api";
import { useProject } from "@/hooks/use-project";

import { WinnowGauge } from "@/components/dashboard/WinnowGauge";
import { TriageFunnelChart } from "@/components/dashboard/TriageFunnelChart";
import { HottestClustersList } from "@/components/dashboard/HottestClustersList";
import { PendingDecisionsCard } from "@/components/dashboard/PendingDecisionsCard";
import { LoadingState, ErrorState, PageHeader } from "@/components/layout/PageState";

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

  const { data, isLoading, error, refetch } = useQuery<DashboardMetrics>({
    queryKey: ["dashboardMetrics", currentProject?.id],
    queryFn: async () => {
      const { data } = await api.get("/dashboard/metrics");
      return data;
    },
    refetchInterval: 30000, // Refresh every 30s
    enabled: !!currentProject, // Only fetch if we have a project selected
  });

  if (isLoading) return <LoadingState message="Calculating metrics..." />;

  if (error) {
    return (
      <ErrorState 
        title="Dashboard Error"
        message={
          (error as { response?: { data?: { message?: string } } }).response?.data?.message || 
          (error as Error).message
        }
        onRetry={() => refetch()}
      />
    );
  }

  if (!data) return null;

  return (
    <div className="flex flex-col gap-6">
      <PageHeader 
        title="Dashboard" 
        description="Real-time triage overview and actionable insights." 
      />

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
