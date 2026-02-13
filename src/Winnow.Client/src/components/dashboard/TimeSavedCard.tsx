
import { Clock } from "lucide-react"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"

interface TimeSavedCardProps {
    hoursSaved: number
}

export function TimeSavedCard({ hoursSaved }: TimeSavedCardProps) {
    return (
        <Card className="h-full bg-white border-gray-200 text-gray-900 shadow-sm dark:bg-[#0F172A] dark:border-white/10 dark:text-white dark:shadow-none transition-colors duration-200 flex flex-col justify-center">
            <CardHeader className="pb-2 border-b border-gray-100 dark:border-white/10">
                <CardTitle className="text-sm font-medium flex items-center gap-2">
                    <Clock className="h-4 w-4 text-gray-500 dark:text-gray-400" />
                    Time Saved
                </CardTitle>
            </CardHeader>
            <CardContent className="pt-8 flex flex-col items-center justify-center flex-grow">
                <div className="flex items-center gap-4">
                    <Clock className="w-10 h-10 text-blue-500/50" />
                    <div className="text-5xl font-bold">{hoursSaved}h</div>
                </div>
                <p className="text-xs text-muted-foreground mt-2">
                    Estimated across all triage actions
                </p>
            </CardContent>
        </Card>
    )
}
