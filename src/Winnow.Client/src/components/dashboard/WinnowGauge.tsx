import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Gauge, Filter } from "lucide-react"

interface WinnowGaugeProps {
    percent: number
    hoursSaved?: number
}

export function WinnowGauge({ percent, hoursSaved }: WinnowGaugeProps) {
    // Rotate based on percentage (0 to 180 degrees for half circle, or full circle)
    // Let's do a simple semi-circle gauge visual
    // const rotation = percent * 180 // Unused for now

    return (
        <Card className="h-full flex flex-col bg-white border-gray-200 text-gray-900 shadow-sm dark:bg-[#0F172A] dark:border-white/10 dark:text-white dark:shadow-none transition-colors duration-200">
            <CardHeader className="pb-2 border-b border-gray-100 dark:border-white/10">
                <CardTitle className="text-sm font-medium flex items-center gap-2">
                    <Filter className="h-4 w-4 text-blue-500" />
                    Winnow Ratio
                </CardTitle>
            </CardHeader>
            <CardContent className="flex-1 flex flex-col items-center justify-center pt-4">
                <div className="relative w-48 h-24 overflow-hidden mb-2">
                    {/* Background Arc */}
                    <div className="absolute top-0 left-0 w-48 h-48 rounded-full border-[12px] border-muted bg-transparent"></div>
                    {/* Active Arc - Simplified CSS for now, or use SVG */}
                    <svg viewBox="0 0 100 50" className="w-full h-full transform transition-all duration-1000 ease-out">
                        <path d="M 10 50 A 40 40 0 0 1 90 50" fill="none" stroke="hsl(var(--muted))" strokeWidth="10" />
                        <path
                            d="M 10 50 A 40 40 0 0 1 90 50"
                            fill="none"
                            stroke="hsl(var(--primary))"
                            strokeWidth="10"
                            strokeDasharray="126"
                            strokeDashoffset={126 - (126 * percent)}
                            strokeLinecap="round"
                        />
                    </svg>

                    <div className="text-center -mt-10">
                        <div className="text-4xl font-bold">{Math.round(percent * 100)}%</div>
                    </div>
                </div>
                <div className="text-center -mt-2 mb-4">
                    <div className="text-xs text-gray-500 dark:text-gray-400 uppercase tracking-widest font-medium">
                        Noise Filtered
                    </div>
                </div>

                {hoursSaved !== undefined && (
                    <div className="flex items-center gap-2 text-sm text-primary font-medium mt-2 bg-primary/10 px-3 py-1 rounded-full">
                        <Gauge className="w-4 h-4" />
                        <span>{hoursSaved} Hours Saved Today</span>
                    </div>
                )}
            </CardContent>
        </Card>
    )
}
