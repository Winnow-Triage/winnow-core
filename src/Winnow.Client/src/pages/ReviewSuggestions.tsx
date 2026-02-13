import { useState, useEffect } from "react"
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query"
import { api } from "@/lib/api"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import { Loader2, CheckCircle, XCircle, SkipForward, ArrowRight, Check } from "lucide-react"
import { toast } from "sonner"
import { useNavigate } from "react-router-dom"
import { cn } from "@/lib/utils"

interface ReviewItem {
    ticketId: string
    ticketTitle: string
    ticketDescription: string
    ticketAuthor: string
    ticketCreatedAt: string
    suggestedParentId: string
    suggestedParentTitle: string
    suggestedParentDescription: string
    confidenceScore: number
}

export default function ReviewSuggestions() {
    const queryClient = useQueryClient()
    const navigate = useNavigate()
    const [currentIndex, setCurrentIndex] = useState(0)

    const { data: queue, isLoading, error } = useQuery<ReviewItem[]>({
        queryKey: ["reviewQueue"],
        queryFn: async () => {
            const res = await api.get("/tickets/review-queue")
            return res.data
        }
    })

    const currentItem = queue && queue.length > 0 ? queue[currentIndex] : null

    const dismissMutation = useMutation({
        mutationFn: async ({ id, reject }: { id: string, reject: boolean }) => {
            await api.post(`/tickets/${id}/dismiss-suggestion`, { rejectMatch: reject })
        },
        onSuccess: () => {
            handleNext()
        }
    })

    const acceptMutation = useMutation({
        mutationFn: async (id: string) => {
            await api.post(`/tickets/${id}/accept-suggestion`, {})
        },
        onSuccess: () => {
            toast.success("Merged into cluster")
            handleNext()
        }
    })

    const handleNext = () => {
        if (!queue) return

        // Optimistic update: remove current item from view
        // Ideally we'd modify the cache, but simply incrementing index or filtering local state works for this flow
        if (currentIndex < queue.length - 1) {
            setCurrentIndex(prev => prev + 1)
        } else {
            // Refetch to see if more came in, or show done state
            queryClient.invalidateQueries({ queryKey: ["reviewQueue"] })
            setCurrentIndex(0) // Reset to 0 to catch any new ones or show empty
        }
    }

    if (isLoading) {
        return (
            <div className="flex h-full items-center justify-center">
                <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
            </div>
        )
    }

    if (!queue || queue.length === 0) {
        return (
            <div className="flex flex-col items-center justify-center h-[80vh] gap-4">
                <div className="rounded-full bg-green-100 p-6 dark:bg-green-900/20">
                    <CheckCircle className="h-12 w-12 text-green-600 dark:text-green-500" />
                </div>
                <h2 className="text-2xl font-bold tracking-tight">All Caught Up!</h2>
                <p className="text-muted-foreground">No more suggestions to review right now.</p>
                <Button onClick={() => navigate("/")}>
                    Back to Dashboard
                </Button>
            </div>
        )
    }

    if (!currentItem) {
        // Fallback if index is out of bounds due to refetch
        return null;
    }

    return (
        <div className="flex flex-col h-full overflow-hidden"> {/* Full height of parent container */}
            <div className="mb-4 px-6 pt-4 shrink-0">
                <h1 className="text-2xl font-bold tracking-tight flex items-center gap-2">
                    Review Suggestions
                    <Badge variant="outline" className="ml-2 font-normal text-sm">
                        {queue.length} Pending
                    </Badge>
                </h1>
            </div>

            <div className="flex-1 min-h-0 grid grid-cols-2 gap-6 px-6 overflow-hidden">
                {/* Left Panel: Incoming Issue */}
                <Card className="h-full flex flex-col border-l-4 border-l-blue-500 overflow-hidden">
                    <CardHeader className="bg-muted/30 pb-4 shrink-0">
                        <div className="flex items-center justify-between">
                            <Badge className="bg-blue-500 text-white hover:bg-blue-600">New Report</Badge>
                            <span className="text-xs text-muted-foreground">
                                {new Date(currentItem.ticketCreatedAt).toLocaleDateString()}
                            </span>
                        </div>
                        <CardTitle className="mt-2 text-xl leading-tight">
                            {currentItem.ticketTitle}
                        </CardTitle>
                        <div className="text-sm text-muted-foreground mt-1">
                            Authored by {currentItem.ticketAuthor}
                        </div>
                    </CardHeader>
                    <CardContent className="flex-1 overflow-y-auto pt-6">
                        <div className="prose prose-sm dark:prose-invert max-w-none">
                            <p className="whitespace-pre-wrap font-mono text-sm leading-relaxed">
                                {currentItem.ticketDescription}
                            </p>
                        </div>
                    </CardContent>
                </Card>

                {/* Right Panel: Suggested Match */}
                <Card className="h-full flex flex-col border-l-4 border-l-purple-500 overflow-hidden relative">
                    <div className="absolute top-0 right-0 p-4 z-10">
                        <Badge variant="secondary" className={cn(
                            "text-xs font-semibold px-2 py-0.5 border",
                            currentItem.confidenceScore > 0.8 ? "bg-green-100 text-green-700 border-green-200 dark:bg-green-900/30 dark:text-green-400" :
                                "bg-yellow-100 text-yellow-700 border-yellow-200 dark:bg-yellow-900/30 dark:text-yellow-400"
                        )}>
                            AI Confidence: {Math.round(currentItem.confidenceScore * 100)}%
                        </Badge>
                    </div>

                    <CardHeader className="bg-purple-50/50 dark:bg-purple-950/10 pb-4 shrink-0">
                        <div className="flex items-center justify-between">
                            <Badge variant="outline" className="border-purple-200 text-purple-700 dark:border-purple-800 dark:text-purple-400">
                                Suggested Parent
                            </Badge>
                        </div>
                        <CardTitle className="mt-2 text-xl leading-tight">
                            {currentItem.suggestedParentTitle}
                        </CardTitle>
                        <div className="text-sm text-muted-foreground mt-1">
                            Existing Cluster Leader
                        </div>
                    </CardHeader>
                    <CardContent className="flex-1 overflow-y-auto pt-6">
                        <div className="prose prose-sm dark:prose-invert max-w-none">
                            <p className="whitespace-pre-wrap font-mono text-sm leading-relaxed">
                                {currentItem.suggestedParentDescription}
                            </p>
                        </div>
                    </CardContent>
                </Card>
            </div>

            {/* Action Bar (Sticky Bottom) */}
            <div className="shrink-0 p-4 mt-4 bg-background border-t shadow-lg flex items-center justify-center gap-4 z-10">
                <div className="flex items-center gap-4 w-full max-w-3xl justify-between">
                    <Button
                        variant="destructive"
                        size="lg"
                        className="w-40 gap-2"
                        onClick={() => dismissMutation.mutate({ id: currentItem.ticketId, reject: true })}
                        disabled={dismissMutation.isPending || acceptMutation.isPending}
                    >
                        <XCircle className="w-4 h-4" />
                        Reject Match
                    </Button>

                    <Button
                        variant="ghost"
                        className="text-muted-foreground hover:text-foreground"
                        onClick={handleNext}
                    >
                        Skip <ArrowRight className="w-4 h-4 ml-1" />
                    </Button>

                    <Button
                        size="lg"
                        className="w-40 gap-2 bg-gradient-to-r from-blue-600 to-purple-600 hover:from-blue-700 hover:to-purple-700 text-white border-0"
                        onClick={() => acceptMutation.mutate(currentItem.ticketId)}
                        disabled={dismissMutation.isPending || acceptMutation.isPending}
                    >
                        <Check className="w-4 h-4" />
                        Confirm Merge
                    </Button>
                </div>
            </div>
        </div>
    )
}
