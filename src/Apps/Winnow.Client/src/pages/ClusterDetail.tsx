import { useState } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import ReactMarkdown from 'react-markdown';
import { useParams, Link, useNavigate } from 'react-router-dom';
import { api } from '@/lib/api';
import { formatTimeAgo } from '@/lib/utils';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Separator } from '@/components/ui/separator';
import { ArrowLeft, Clock, Sparkles, LayoutDashboard, RotateCw, Trash2, MoreHorizontal } from 'lucide-react';
import {
    DropdownMenu,
    DropdownMenuContent,
    DropdownMenuItem,
    DropdownMenuSeparator,
    DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import {
    AlertDialog,
    AlertDialogAction,
    AlertDialogCancel,
    AlertDialogContent,
    AlertDialogDescription,
    AlertDialogFooter,
    AlertDialogHeader,
    AlertDialogTitle,
} from "@/components/ui/alert-dialog";

interface ClusterMember {
    id: string;
    title: string;
    message: string;
    status: string;
    createdAt: string;
    confidenceScore?: number;
}

interface ClusterDetailData {
    id: string;
    projectId: string;
    title?: string;
    summary?: string;
    criticalityScore?: number;
    criticalityReasoning?: string;
    status: string;
    createdAt: string;
    reports: ClusterMember[];
}

export default function ClusterDetail() {
    const { id } = useParams<{ id: string }>();
    const navigate = useNavigate();
    const queryClient = useQueryClient();
    const [isGeneratingSummary, setIsGeneratingSummary] = useState(false);
    const [isClearingSummary, setIsClearingSummary] = useState(false);
    const [confirmAction, setConfirmAction] = useState<{
        isOpen: boolean;
        title: string;
        description: string;
        action: () => Promise<void>;
    }>({ isOpen: false, title: '', description: '', action: async () => { } });

    const { data: cluster, isLoading, error } = useQuery<ClusterDetailData>({
        queryKey: ['cluster', id],
        queryFn: async () => {
            const { data } = await api.get(`/clusters/${id}`);
            return data;
        },
        enabled: !!id,
    });

    if (isLoading) return <div className="p-8">Loading cluster details...</div>;
    if (error || !cluster) return <div className="p-8 text-red-500">Error loading cluster.</div>;

    const handleGenerateSummary = async () => {
        setIsGeneratingSummary(true);
        try {
            await api.post(`/reports/${cluster.reports[0].id}/generate-summary`, {});
            queryClient.invalidateQueries({ queryKey: ['cluster', id] });
        } catch (e) {
            console.error("Failed to generate summary", e);
        } finally {
            setIsGeneratingSummary(false);
        }
    };

    const handleClearSummary = async () => {
        setIsClearingSummary(true);
        try {
            await api.post(`/reports/${cluster.reports[0].id}/clear-summary`, {});
            queryClient.invalidateQueries({ queryKey: ['cluster', id] });
        } catch (e) {
            console.error("Failed to clear summary", e);
        } finally {
            setIsClearingSummary(false);
        }
    };

    return (
        <div className="flex flex-col gap-6 max-w-5xl mx-auto w-full">
            <div className="flex items-center gap-4">
                <Button variant="ghost" size="icon" onClick={() => navigate(-1)}>
                    <ArrowLeft className="h-4 w-4" />
                </Button>
                <div>
                    <div className="flex items-center gap-2">
                        <h1 className="text-2xl font-bold tracking-tight">Cluster: {cluster.title || cluster.id.substring(0, 8)}</h1>
                        <Badge variant={cluster.status === 'Closed' ? 'secondary' : 'default'}>
                            {cluster.status}
                        </Badge>
                    </div>
                </div>
                <div className="ml-auto flex items-center gap-2">
                    <Button variant="outline" onClick={() => {
                        setConfirmAction({
                            isOpen: true,
                            title: 'Close Cluster?',
                            description: 'Are you sure you want to CLOSE ALL reports in this cluster?',
                            action: async () => {
                                await api.post(`/reports/${cluster.reports[0].id}/close-cluster`, {});
                                queryClient.invalidateQueries({ queryKey: ['cluster', id] });
                            }
                        });
                    }}>
                        Close Cluster
                    </Button>
                </div>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
                <div className="md:col-span-2 flex flex-col gap-6">
                    {/* AI Perspective Card */}
                    <Card className="border-purple-200 dark:border-purple-500/30 bg-purple-50/50 dark:bg-[#160d33] shadow-xl relative overflow-hidden">
                        <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2 relative z-10">
                            <div className="flex flex-col gap-1">
                                <CardTitle className="text-xl font-bold flex items-center gap-2 tracking-tight text-purple-900 dark:text-white">
                                    <Sparkles className="h-5 w-5 text-purple-600 dark:text-purple-400" />
                                    AI Perspective
                                </CardTitle>
                                {cluster.criticalityScore && (
                                    <div className="flex items-center gap-2">
                                        <Badge variant="outline" className={`
                                            ${cluster.criticalityScore >= 8 ? 'bg-red-100 text-red-800 border-red-200 dark:bg-red-900/30 dark:text-red-300 dark:border-red-800' :
                                                cluster.criticalityScore >= 5 ? 'bg-amber-100 text-amber-800 border-amber-200 dark:bg-amber-900/30 dark:text-amber-300 dark:border-amber-800' :
                                                    'bg-blue-100 text-blue-800 border-blue-200 dark:bg-blue-900/30 dark:text-blue-300 dark:border-blue-800'}
                                        `}>
                                            Criticality: {cluster.criticalityScore}/10
                                        </Badge>
                                    </div>
                                )}
                            </div>
                            {!cluster.summary ? (
                                <Button
                                    size="sm"
                                    variant="outline"
                                    disabled={isGeneratingSummary}
                                    onClick={handleGenerateSummary}
                                >
                                    {isGeneratingSummary ? 'Analyzing...' : 'Analyze with AI'}
                                </Button>
                            ) : (
                                <DropdownMenu>
                                    <DropdownMenuTrigger asChild>
                                        <Button variant="ghost" size="icon">
                                            <MoreHorizontal className="h-4 w-4" />
                                        </Button>
                                    </DropdownMenuTrigger>
                                    <DropdownMenuContent align="end">
                                        <DropdownMenuItem disabled={isGeneratingSummary} onClick={handleGenerateSummary}>
                                            <RotateCw className={`mr-2 h-4 w-4 ${isGeneratingSummary ? 'animate-spin' : ''}`} />
                                            Regenerate Summary
                                        </DropdownMenuItem>
                                        <DropdownMenuSeparator />
                                        <DropdownMenuItem className="text-red-600" disabled={isClearingSummary} onClick={handleClearSummary}>
                                            <Trash2 className="mr-2 h-4 w-4" />
                                            Clear Summary
                                        </DropdownMenuItem>
                                    </DropdownMenuContent>
                                </DropdownMenu>
                            )}
                        </CardHeader>
                        <CardContent className="relative min-h-[120px] z-10 pt-4">
                            {cluster.summary ? (
                                <div className="prose prose-sm max-w-none dark:prose-invert">
                                    <ReactMarkdown>{cluster.summary}</ReactMarkdown>
                                    {cluster.criticalityReasoning && (
                                        <div className="mt-4 p-3 bg-white/50 dark:bg-black/20 rounded-lg text-sm italic border-l-4 border-purple-400">
                                            "{cluster.criticalityReasoning}"
                                        </div>
                                    )}
                                </div>
                            ) : (
                                <div className="flex flex-col items-center justify-center py-8 text-center text-muted-foreground">
                                    <Sparkles className="h-8 w-8 mb-2 opacity-20" />
                                    <p>No AI Insight Available</p>
                                </div>
                            )}
                        </CardContent>
                    </Card>

                    <Card>
                        <CardHeader>
                            <CardTitle className="text-lg flex items-center gap-2">
                                <Clock className="h-5 w-5" />
                                Report History
                                <Badge variant="secondary" className="ml-auto">{cluster.reports.length}</Badge>
                            </CardTitle>
                        </CardHeader>
                        <CardContent>
                            <div className="flex flex-col gap-3">
                                {cluster.reports.map((report) => (
                                    <div key={report.id} className="p-4 border rounded-lg hover:bg-muted/50 transition-all group flex items-center justify-between gap-4">
                                        <div className="flex-1 min-w-0">
                                            <div className="flex items-center gap-2 mb-1">
                                                <Link to={`/reports/${report.id}`} className="font-semibold truncate hover:underline text-blue-600 dark:text-blue-400">
                                                    {report.title || report.message}
                                                </Link>
                                                <Badge variant="outline" className="text-[10px] uppercase font-bold tracking-tight">
                                                    {report.status}
                                                </Badge>
                                            </div>
                                            <div className="text-xs text-muted-foreground flex items-center gap-2">
                                                <span>{formatTimeAgo(report.createdAt)}</span>
                                                {report.confidenceScore && (
                                                    <span className="text-blue-500 font-medium">
                                                        {(report.confidenceScore * 100).toFixed(0)}% Similarity
                                                    </span>
                                                )}
                                            </div>
                                        </div>
                                        <Button variant="ghost" size="sm" asChild>
                                            <Link to={`/reports/${report.id}`}>
                                                View Report
                                            </Link>
                                        </Button>
                                    </div>
                                ))}
                            </div>
                        </CardContent>
                    </Card>
                </div>

                <div className="flex flex-col gap-6">
                    <Card>
                        <CardHeader>
                            <CardTitle className="text-sm font-medium">Cluster Metadata</CardTitle>
                        </CardHeader>
                        <CardContent className="flex flex-col gap-4 text-sm">
                            <div className="flex justify-between">
                                <span className="text-muted-foreground flex items-center gap-2">
                                    <Clock className="h-3 w-3" /> Created
                                </span>
                                <span>{new Date(cluster.createdAt).toLocaleDateString()}</span>
                            </div>
                            <Separator />
                            <div className="flex justify-between">
                                <span className="text-muted-foreground flex items-center gap-2">
                                    <LayoutDashboard className="h-3 w-3" /> Population
                                </span>
                                <span>{cluster.reports.length} reports</span>
                            </div>
                        </CardContent>
                    </Card>
                </div>
            </div>

            <AlertDialog open={confirmAction.isOpen} onOpenChange={(open) => setConfirmAction(p => ({ ...p, isOpen: open }))}>
                <AlertDialogContent>
                    <AlertDialogHeader>
                        <AlertDialogTitle>{confirmAction.title}</AlertDialogTitle>
                        <AlertDialogDescription>{confirmAction.description}</AlertDialogDescription>
                    </AlertDialogHeader>
                    <AlertDialogFooter>
                        <AlertDialogCancel>Cancel</AlertDialogCancel>
                        <AlertDialogAction onClick={confirmAction.action}>Confirm</AlertDialogAction>
                    </AlertDialogFooter>
                </AlertDialogContent>
            </AlertDialog>
        </div>
    );
}
