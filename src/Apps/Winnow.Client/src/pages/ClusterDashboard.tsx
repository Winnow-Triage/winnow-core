import { useQuery } from "@tanstack/react-query"
import { api } from "@/lib/api"
import { useProject } from "@/context/ProjectContext"
import { AlertCircle, Loader2 } from "lucide-react"
import { useTheme } from "@/components/theme-provider"

import { WinnowGauge } from "@/components/dashboard/WinnowGauge"
import { TriageFunnelChart } from "@/components/dashboard/TriageFunnelChart"
import { HottestClustersList } from "@/components/dashboard/HottestClustersList"
import { PendingDecisionsCard } from "@/components/dashboard/PendingDecisionsCard"
import { TimeSavedCard } from "@/components/dashboard/TimeSavedCard"

// DTO types matching backend
interface DashboardMetrics {
    triage: {
        totalReports: number
        activeClusters: number
        noiseReductionRatio: number
        pendingReviews: number
        estimatedHoursSaved: number
    }
    trendingClusters: {
        clusterId: string
        title: string
        status: string
        reportCount: number
        velocity: number
        isHot: boolean
    }[]
    volumeHistory: {
        timestamp: string
        newUniqueCount: number
        duplicateCount: number
    }[]
}

export default function ClusterDashboard() {
    const { theme } = useTheme()
    const { currentProject } = useProject()

    // Resolve effective theme
    const isDark = theme === 'dark' ||
        (theme === 'system' && window.matchMedia('(prefers-color-scheme: dark)').matches)

    const { data, isLoading, error } = useQuery<DashboardMetrics>({
        queryKey: ["dashboardMetrics", currentProject?.id],
        queryFn: async () => {
            // Use configured api client which handles base URL and tenant headers
            const res = await api.get("/dashboard/metrics")
            return res.data
        },
        refetchInterval: 30000, // Refresh every 30s
        enabled: !!currentProject
    })

    if (isLoading) {
        return (
            <div className="flex h-full items-center justify-center">
                <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
            </div>
        )
    }


    if (error) {
        return (
            <div className="p-8">
                <div className="bg-destructive/15 text-destructive p-4 rounded-md border border-destructive/20 flex gap-2 items-center">
                    <AlertCircle className="h-4 w-4" />
                    <div>
                        <p className="font-semibold">Error</p>
                        <p className="text-sm">Failed to load dashboard metrics. {(error as Error).message}</p>
                    </div>
                </div>
            </div>
        )
    }

    if (!data) return null;

    return (
        <div className="space-y-6 pt-4">
            <div>
                <h1 className="text-3xl font-bold tracking-tight">Actionable Triage</h1>
                <p className="text-muted-foreground">Real-time noise (duplicate) reduction and operational insights.</p>
            </div>

            <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
                {/* Winnow Gauge (Hero Metric) */}
                <div className="col-span-1">
                    <WinnowGauge
                        percent={data.triage.noiseReductionRatio}
                        // Remove hoursSaved from here as it has its own card now
                        hoursSaved={undefined}
                    />
                </div>

                {/* Pending Decisions (Compact) */}
                <div className="col-span-1">
                    <PendingDecisionsCard count={data.triage.pendingReviews} />
                </div>

                {/* Time Saved (New Card) */}
                <div className="col-span-1">
                    <TimeSavedCard hoursSaved={data.triage.estimatedHoursSaved} />
                </div>
            </div>

            <div className="grid gap-6 grid-cols-1 lg:grid-cols-10">
                {/* Triage Funnel (Main Chart) - 70% width */}
                <div className="lg:col-span-7">
                    <TriageFunnelChart
                        data={data.volumeHistory}
                        noiseColor={isDark ? "#374151" : "#E5E7EB"}
                        signalColor="#3B82F6"
                    />
                </div>

                {/* Actionable Cards (Right Column) - 30% width */}
                <div className="lg:col-span-3">
                    <HottestClustersList clusters={data.trendingClusters} />
                </div>
            </div>
        </div>
    )
}
