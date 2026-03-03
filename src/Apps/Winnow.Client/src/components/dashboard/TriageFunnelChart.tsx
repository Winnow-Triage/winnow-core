import { useChartDimensions } from "@/hooks/use-chart-dimensions"
import { Area, AreaChart, CartesianGrid, XAxis, YAxis, Tooltip } from "recharts"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Activity } from "lucide-react"

interface TriageFunnelChartProps {
    data: any[]
    noiseColor: string
    signalColor: string
}

export function TriageFunnelChart({ data, noiseColor, signalColor }: TriageFunnelChartProps) {
    // Transform timestamp if needed
    const chartData = data.map(item => ({
        ...item,
        time: new Date(item.timestamp).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })
    }))

    const { ref, dimensions } = useChartDimensions()

    return (
        <Card className="col-span-2">
            <CardHeader className="flex flex-row items-center justify-between space-y-0">
                <CardTitle className="flex items-center gap-2">
                    <Activity className="h-4 w-4 text-blue-500" />
                    Triage Funnel
                </CardTitle>
                <div className="flex items-center gap-4 text-xs font-bold uppercase tracking-widest opacity-70">
                    <div className="flex items-center gap-2">
                        <div className="w-3 h-3 rounded-full bg-blue-500"></div>
                        <span>Unique Issues</span>
                    </div>
                    <div className="flex items-center gap-2">
                        <div className="w-3 h-3 rounded-full bg-gray-500"></div>
                        <span>Duplicates</span>
                    </div>
                </div>
            </CardHeader>
            <CardContent className="pt-6">
                <div ref={ref} className="h-[200px] w-full relative">
                    {dimensions.width > 0 && dimensions.height > 0 ? (
                        <AreaChart
                            id="triage-funnel-area-chart"
                            width={dimensions.width}
                            height={dimensions.height}
                            data={chartData}
                            margin={{ top: 10, right: 30, left: 0, bottom: 0 }}
                        >
                            <defs>
                                <linearGradient id="colorDup" x1="0" y1="0" x2="0" y2="1">
                                    <stop offset="5%" stopColor={noiseColor} stopOpacity={0.4} />
                                    <stop offset="95%" stopColor={noiseColor} stopOpacity={0.1} />
                                </linearGradient>
                                <linearGradient id="colorUnique" x1="0" y1="0" x2="0" y2="1">
                                    <stop offset="5%" stopColor={signalColor} stopOpacity={0.8} />
                                    <stop offset="95%" stopColor={signalColor} stopOpacity={0.1} />
                                </linearGradient>
                            </defs>
                            <XAxis
                                dataKey="time"
                                stroke="#888888"
                                fontSize={12}
                                tickLine={false}
                                axisLine={false}
                                hide={dimensions.width < 300}
                            />
                            <YAxis stroke="#888888" fontSize={12} tickLine={false} axisLine={false} tickFormatter={(value) => `${value}`} />
                            <Tooltip
                                contentStyle={{
                                    backgroundColor: 'var(--color-card)',
                                    borderColor: 'var(--color-border)',
                                    color: 'var(--color-card-foreground)'
                                }}
                                itemStyle={{ color: 'var(--color-card-foreground)' }}
                            />
                            <CartesianGrid strokeDasharray="3 3" stroke="#88888833" vertical={false} />
                            <Area
                                type="monotone"
                                dataKey="newUniqueCount"
                                stackId="1"
                                stroke={signalColor}
                                fill="url(#colorUnique)"
                                name="Signal (New Issues)"
                            />
                            <Area
                                type="monotone"
                                dataKey="duplicateCount"
                                stackId="1"
                                stroke={noiseColor}
                                fill="url(#colorDup)"
                                fillOpacity={1}
                                name="Noise (Duplicates)"
                            />
                        </AreaChart>
                    ) : (
                        <div className="h-full w-full bg-gray-50/50 dark:bg-white/5 animate-pulse rounded-md" />
                    )}
                </div>
            </CardContent>
        </Card>
    )
}
