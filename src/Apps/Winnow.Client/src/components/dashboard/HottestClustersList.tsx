import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Badge } from "@/components/ui/badge"
import { Flame, ArrowUpRight } from "lucide-react"

interface Cluster {
    clusterId: string
    title: string
    ticketCount: number
    velocity: number
    isHot: boolean
}

interface HottestClustersListProps {
    clusters: Cluster[]
}

export function HottestClustersList({ clusters }: HottestClustersListProps) {
    return (
        <Card className="h-full bg-white border-gray-200 text-gray-900 shadow-sm dark:bg-[#0F172A] dark:border-white/10 dark:text-white dark:shadow-none transition-colors duration-200">
            <CardHeader className="border-b border-gray-100 dark:border-white/10">
                <CardTitle className="flex items-center gap-2">
                    <Flame className="w-5 h-5 text-orange-500" />
                    Hottest Clusters
                </CardTitle>
            </CardHeader>
            <CardContent className="grid gap-1 pt-4">
                {clusters.length === 0 && <p className="text-muted-foreground text-sm">No trending clusters right now.</p>}
                {clusters.map((cluster) => (
                    <div key={cluster.clusterId} className="flex items-start justify-between space-x-4 p-2 rounded-lg hover:bg-muted/50 transition-colors cursor-pointer even:bg-gray-50 dark:even:bg-white/5">
                        <div className="space-y-1 overflow-hidden">
                            <div className="flex items-center gap-2">
                                <p className="text-sm font-medium leading-none truncate max-w-[200px] text-gray-900 dark:text-white">{cluster.title}</p>
                                {cluster.isHot && <Badge variant="destructive" className="text-[10px] h-4 px-1">HOT</Badge>}
                            </div>
                            <p className="text-xs text-gray-500 dark:text-gray-400">{cluster.ticketCount} Total Reports</p>
                        </div>
                        <div className="flex items-center gap-1 text-green-500 text-xs font-medium bg-green-500/10 px-2 py-1 rounded">
                            <ArrowUpRight className="w-3 h-3" />
                            <span>+{cluster.velocity} / hr</span>
                        </div>
                    </div>
                ))}
            </CardContent>
        </Card>
    )
}
