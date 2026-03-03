import React, { useState } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { useParams, Link, useNavigate } from 'react-router-dom';
import { api } from '@/lib/api';
import { formatTimeAgo, cn } from '@/lib/utils';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Separator } from '@/components/ui/separator';
import { ArrowLeft, ExternalLink, MessageSquare, Clock, AlertCircle, AlertTriangle, Sparkles, Paperclip, ShieldCheck, ShieldAlert, Loader2 } from 'lucide-react';
import { MediaGallery } from '@/components/MediaGallery';
import { ConsoleLogsCard } from '@/components/dashboard/ConsoleLogsCard';
import { PageTitle } from '@/components/ui/page-title';
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

interface RelatedReport {
    id: string;
    message: string;
    status: string;
    createdAt: string;
    confidenceScore?: number;
}

interface AssetData {
    id: string;
    fileName: string;
    contentType: string;
    sizeBytes: number;
    status: string; // Pending, Clean, Infected, Failed
    downloadUrl?: string;
    createdAt: string;
    scannedAt?: string;
}

interface ReportDetailData {
    id: string;
    title: string;
    message: string;
    stackTrace: string;
    status: string;
    createdAt: string;
    projectId: string;
    clusterId?: string;
    clusterTitle?: string;
    suggestedClusterId?: string;
    suggestedConfidenceScore?: number;
    suggestedClusterSummary?: string;
    suggestedClusterTitle?: string;
    assignedTo?: string;
    summary?: string;
    confidenceScore?: number;
    criticalityScore?: number;
    criticalityReasoning?: string;
    metadata?: string;
    externalUrl?: string;
    isOverage?: boolean;
    isLocked?: boolean;
    assets: AssetData[];
    evidence: RelatedReport[];
}

export default function ReportDetail() {
    const { id } = useParams<{ id: string }>();
    const navigate = useNavigate();
    const queryClient = useQueryClient();
    const [confirmAction, setConfirmAction] = useState<{
        isOpen: boolean;
        title: string;
        description: string;
        action: () => Promise<void>;
    }>({ isOpen: false, title: '', description: '', action: async () => { } });

    const { data: report, isLoading, error } = useQuery<ReportDetailData>({
        queryKey: ['report', id],
        queryFn: async () => {
            const { data } = await api.get(`/reports/${id}`);
            return data;
        },
        enabled: !!id,
    });

    if (isLoading) return <div className="p-8">Loading report details...</div>;
    if (error || !report) return <div className="p-8 text-red-500">Error loading report.</div>;

    return (
        <div className="flex flex-col gap-6 max-w-5xl mx-auto w-full">
            {/* Header / Navigation */}
            <div className="flex items-center gap-4">
                <Button variant="ghost" size="icon" onClick={() => navigate(-1)}>
                    <ArrowLeft className="h-4 w-4" />
                </Button>
                <div className="flex flex-col gap-1 flex-1">
                    <div className="flex items-center gap-2">
                        <span className="text-xs font-mono text-muted-foreground bg-muted/50 px-2 py-0.5 rounded-md">
                            R-{report.id.substring(0, 8)}
                        </span>
                        <Badge variant="neutral" className="h-5 px-2 text-[10px] uppercase tracking-wider">
                            {report.status}
                        </Badge>
                        {report.assignedTo && (
                            <Badge variant="outline" className="h-5 px-2 text-[10px] bg-blue-500/5 text-blue-600 border-blue-200 dark:border-blue-900">
                                {report.assignedTo}
                            </Badge>
                        )}
                    </div>
                    <PageTitle className="text-3xl md:text-4xl">
                        {report.title}
                    </PageTitle>
                </div>
                <div className="ml-auto flex items-center gap-2">
                    {/* External Link Button - Only visible if URL exists */}
                    {report.externalUrl && (
                        <Button variant="outline" asChild>
                            <a href={report.externalUrl} target="_blank" rel="noopener noreferrer">
                                <ExternalLink className="mr-2 h-4 w-4" />
                                Open in External System
                            </a>
                        </Button>
                    )}
                </div>
            </div>

            {/* Duplicate Alert */}
            {report.clusterId && report.status === 'Duplicate' && (
                <div className="bg-amber-500/10 backdrop-blur-md text-amber-900 dark:text-amber-200 border border-amber-200 dark:border-amber-800/50 rounded-3xl p-6 flex items-center gap-4 shadow-xl shadow-amber-500/5">
                    <div className="bg-amber-500/20 p-3 rounded-2xl">
                        <AlertCircle className="h-6 w-6 text-amber-600 dark:text-amber-400" />
                    </div>
                    <div className="flex-1">
                        <div className="flex items-center gap-3 mb-1">
                            <h4 className="font-bold text-lg">This report belongs to a cluster</h4>
                            <Badge variant="outline" className={cn(
                                "rounded-full border-amber-200 bg-amber-500/5 px-3 py-0.5 text-xs font-semibold",
                                report.confidenceScore && report.confidenceScore > 0.8 ? 'text-emerald-600 border-emerald-200 bg-emerald-500/5' : 'text-amber-600'
                            )}>
                                {report.confidenceScore !== undefined && report.confidenceScore !== null
                                    ? `${(report.confidenceScore * 100).toFixed(0)}% Match Confidence`
                                    : 'Confidence: N/A'}
                            </Badge>
                        </div>
                        <p className="text-sm opacity-80">
                            Cluster: <Link to={`/clusters/${report.clusterId}`} className="font-semibold underline decoration-amber-500/30 underline-offset-4 hover:decoration-amber-500 transition-all">
                                {report.clusterTitle || report.clusterId}
                            </Link>
                        </p>
                    </div>
                    <div className="flex gap-2">
                        <Button
                            variant="ghost"
                            size="sm"
                            className="shrink-0 hover:bg-amber-200 dark:hover:bg-amber-800/50 text-amber-800 dark:text-amber-200"
                            onClick={() => {
                                setConfirmAction({
                                    isOpen: true,
                                    title: 'Ungroup Report?',
                                    description: 'Are you sure you want to ungroup this report? It will be treated as specific unique issue.',
                                    action: async () => {
                                        await api.post(`/reports/${report.id}/ungroup`, {});
                                        queryClient.invalidateQueries({ queryKey: ['report', id] });
                                        queryClient.invalidateQueries({ queryKey: ['reports'] });
                                    }
                                });
                            }}
                        >
                            Ungroup
                        </Button>
                    </div>
                </div>
            )}

            {/* Paywall Banner (Retroactive Data Ransom) */}
            {report.isLocked && (
                <div className="bg-red-500/10 backdrop-blur-md text-red-900 dark:text-red-200 border border-red-200/50 dark:border-red-800/50 rounded-3xl p-6 flex items-center gap-4 shadow-xl shadow-red-500/5">
                    <div className="bg-red-500/20 p-3 rounded-2xl">
                        <ShieldAlert className="h-8 w-8 text-red-600 dark:text-red-400 shrink-0" />
                    </div>
                    <div className="flex-1">
                        <h4 className="font-bold text-xl tracking-tight">Report Locked</h4>
                        <p className="text-sm opacity-80 mt-1 leading-relaxed max-w-2xl">
                            Your organization has exceeded its monthly ingestion limit. Upgrade to a higher tier to view this stack trace and unlock your remaining reports.
                        </p>
                    </div>
                    <Button asChild className="shrink-0 bg-red-600 hover:bg-red-700 text-white rounded-2xl h-12 px-6 font-bold shadow-lg shadow-red-500/20 transition-all hover:scale-105 active:scale-95">
                        <Link to="/settings?tab=billing">
                            Upgrade to Unlock
                        </Link>
                    </Button>
                </div>
            )}

            {/* Overage Warning Banner */}
            {!report.isLocked && report.isOverage && (
                <div className="bg-amber-50 dark:bg-amber-900/20 text-amber-800 dark:text-amber-200 border border-amber-200 dark:border-amber-800 rounded-lg p-3 flex items-center gap-3 text-sm">
                    <AlertTriangle className="h-5 w-5 text-amber-600 dark:text-amber-400 shrink-0" />
                    <p className="flex-1">
                        <strong>Heads up:</strong> You've exceeded your tier's monthly ingestion limit. We're still capturing your data, but further reports will be locked soon.
                    </p>
                    <Button variant="outline" size="sm" asChild className="shrink-0 border-amber-300 dark:border-amber-700 hover:bg-amber-100 dark:hover:bg-amber-800 bg-transparent text-amber-900 dark:text-amber-100">
                        <Link to="/settings?tab=billing">
                            Manage Subscription
                        </Link>
                    </Button>
                </div>
            )}

            {/* Suggested Match Alert */}
            {report.suggestedClusterId && (
                <div className="bg-blue-500/10 backdrop-blur-md text-blue-900 dark:text-blue-200 border border-blue-200/50 dark:border-blue-800/50 rounded-3xl p-6 flex items-center gap-4 shadow-xl shadow-blue-500/5">
                    <div className="bg-blue-500/20 p-3 rounded-2xl">
                        <Sparkles className="h-6 w-6 text-blue-600 dark:text-blue-400" />
                    </div>
                    <div className="flex-1">
                        <div className="flex items-center gap-3 mb-1">
                            <h4 className="font-bold text-lg">Suggested Cluster Match</h4>
                            <Badge variant="outline" className="rounded-full border-blue-200 bg-blue-500/5 px-3 py-0.5 text-xs font-semibold text-blue-600">
                                {report.suggestedConfidenceScore !== undefined && report.suggestedConfidenceScore !== null
                                    ? `${(report.suggestedConfidenceScore * 100).toFixed(0)}% Similarity`
                                    : 'Similarity: N/A'}
                            </Badge>
                        </div>
                        <p className="text-sm opacity-80">
                            This report looks very similar to cluster: <span className="font-bold">
                                {report.suggestedClusterTitle || report.suggestedClusterSummary || `Cluster #${report.suggestedClusterId?.substring(0, 8)}`}
                            </span>.
                        </p>
                    </div>
                    <div className="flex gap-2">
                        <Button
                            variant="default"
                            size="sm"
                            className="bg-blue-600 hover:bg-blue-700 text-white rounded-2xl h-10 px-4 font-bold transition-all hover:scale-105"
                            onClick={async () => {
                                setConfirmAction({
                                    isOpen: true,
                                    title: 'Accept Suggested Match?',
                                    description: `Are you sure you want to add this report to the suggested cluster?`,
                                    action: async () => {
                                        await api.post(`/reports/${report.id}/accept-suggestion`, {});
                                        queryClient.invalidateQueries({ queryKey: ['report', id] });
                                        queryClient.invalidateQueries({ queryKey: ['reports'] });
                                    }
                                });
                            }}
                        >
                            Accept Suggestion
                        </Button>
                        <Button
                            variant="ghost"
                            size="sm"
                            className="text-blue-800 dark:text-blue-200 hover:bg-blue-500/10 rounded-2xl h-10 px-4 font-medium transition-all"
                            onClick={async () => {
                                try {
                                    await api.post(`/reports/${report.id}/dismiss-suggestion`, {});
                                    queryClient.invalidateQueries({ queryKey: ['report', id] });
                                } catch (e) {
                                    console.error("Failed to dismiss suggestion", e);
                                }
                            }}
                        >
                            Dismiss
                        </Button>
                    </div>
                </div>
            )}

            <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
                {/* Main Content: Master Description */}
                <div className="md:col-span-2 flex flex-col gap-6">
                    {/* Header Section */}
                    <div className="flex flex-col gap-2">
                        <div className="flex items-center gap-2 text-sm text-gray-500 dark:text-gray-400 px-2">
                            <span>
                                Authored by {(() => {
                                    try {
                                        const meta = report.metadata ? JSON.parse(report.metadata) : {};
                                        return meta.user || meta.author || meta.username || 'Unassigned';
                                    } catch {
                                        return 'Unassigned';
                                    }
                                })()}
                            </span>
                            <span>•</span>
                            <span>{formatTimeAgo(report.createdAt)}</span>
                        </div>
                    </div>
                    {/* AI Summary Section - Show if summary exists or if it's a cluster parent */}
                    {/* AI Perspective moved to Cluster Detail */}
                    {report.clusterId && (
                        <div className="flex items-center gap-4 p-5 bg-purple-500/5 backdrop-blur-sm border border-purple-200/50 dark:border-purple-800/30 rounded-3xl shadow-lg shadow-purple-500/5">
                            <div className="bg-purple-500/20 p-2 rounded-xl">
                                <Sparkles className="h-5 w-5 text-purple-600 dark:text-purple-400" />
                            </div>
                            <div className="flex-1 text-sm leading-relaxed">
                                <span className="font-bold text-purple-900 dark:text-purple-300">AI Analysis Available:</span> This report is part of a cluster with an AI-generated summary and criticality analysis.
                            </div>
                            <Button variant="outline" size="sm" asChild className="rounded-2xl border-purple-200 hover:bg-purple-100 dark:border-purple-800 dark:text-purple-300 transition-all font-semibold">
                                <Link to={`/clusters/${report.clusterId}`}>
                                    View Cluster Analysis
                                </Link>
                            </Button>
                        </div>
                    )}



                    <Card className="rounded-3xl overflow-hidden border-white/10 shadow-2xl backdrop-blur-sm">
                        <CardHeader className="bg-muted/30 pb-4">
                            <CardTitle className="text-lg font-bold flex items-center gap-2">
                                <MessageSquare className="h-5 w-5 text-blue-500" />
                                User Description
                            </CardTitle>
                        </CardHeader>
                        <CardContent className="pt-6">
                            <div className={`prose dark:prose-invert max-w-none ${report.isLocked ? 'blur-md select-none pointer-events-none opacity-60' : ''}`}>
                                <p className="whitespace-pre-wrap leading-relaxed text-gray-700 dark:text-gray-300">
                                    {report.isLocked ? 'This content is hidden. Please upgrade your plan to unlock this data.' : report.message}
                                </p>
                            </div>
                        </CardContent>
                    </Card>

                    <Card className="rounded-3xl overflow-hidden border-white/10 shadow-2xl backdrop-blur-sm">
                        <CardHeader className="bg-muted/30 pb-4">
                            <CardTitle className="text-lg font-bold flex items-center gap-2">
                                <Clock className="h-5 w-5 text-emerald-500" />
                                Activity Log
                            </CardTitle>
                        </CardHeader>
                        <CardContent className="pt-6">
                            <div className="text-sm text-muted-foreground italic">
                                No activity recorded yet.
                            </div>
                        </CardContent>
                    </Card>
                </div>

                {/* Sidebar: Meta & Evidence */}
                <div className="flex flex-col gap-6">
                    <Card>
                        <CardHeader>
                            <CardTitle className="text-sm font-medium">Metadata</CardTitle>
                        </CardHeader>
                        <CardContent className="flex flex-col gap-4 text-sm">
                            <div className="flex justify-between">
                                <span className="text-muted-foreground flex items-center gap-2">
                                    <Clock className="h-3 w-3" /> Created
                                </span>
                                <span className="text-right">{new Date(report.createdAt).toLocaleString()}</span>
                            </div>
                            <Separator />
                            <div className="flex justify-between">
                                <span className="text-muted-foreground flex items-center gap-2">
                                    <AlertCircle className="h-3 w-3" /> Priority
                                </span>
                                <span>Normal</span>
                            </div>
                            <Separator />
                            <div className="flex justify-between">
                                <span className="text-muted-foreground flex items-center gap-2">
                                    <MessageSquare className="h-3 w-3" /> Comments
                                </span>
                                <span>0</span>
                            </div>
                            {report.metadata && (
                                <>
                                    <Separator />
                                    <div className="flex flex-col gap-2">
                                        <span className="text-muted-foreground flex items-center gap-2">
                                            <Sparkles className="h-3 w-3" /> Session Context
                                        </span>
                                        <div className="bg-muted/50 p-2 rounded-md grid grid-cols-2 gap-x-4 gap-y-1">
                                            {(() => {
                                                try {
                                                    const metadata = JSON.parse(report.metadata);
                                                    return Object.entries(metadata)
                                                        .filter(([key]) => key !== 'logs' && key !== 'context') // Filter logs and generic context if redundant
                                                        .map(([key, value]) => (
                                                            <React.Fragment key={key}>
                                                                <span className="text-xs font-medium text-muted-foreground uppercase">{key}</span>
                                                                <span className="text-xs text-right font-mono truncate" title={String(value)}>{String(value)}</span>
                                                            </React.Fragment>
                                                        ));
                                                } catch (e) {
                                                    return <span className="text-xs text-red-500">Error parsing metadata</span>;
                                                }
                                            })()}
                                        </div>
                                    </div>
                                </>
                            )}
                        </CardContent>
                    </Card>

                    {/* Evidence Locker moved to Cluster Detail */}

                    {/* Attachments / Assets */}
                    {report.assets && report.assets.length > 0 && (
                        <Card className="rounded-3xl border-white/10 shadow-2xl overflow-hidden">
                            <CardHeader className="bg-muted/30">
                                <CardTitle className="text-sm font-bold flex items-center gap-2">
                                    <Paperclip className="h-4 w-4 text-blue-500" />
                                    Attachments
                                    <Badge variant="neutral" className="ml-auto text-xs h-5">
                                        {report.assets.length} file{report.assets.length > 1 ? 's' : ''}
                                    </Badge>
                                </CardTitle>
                                <CardDescription className="text-xs">Files captured with this report.</CardDescription>
                            </CardHeader>
                            <CardContent className={cn(
                                "space-y-4 pt-6",
                                report.isLocked ? 'blur-md select-none pointer-events-none opacity-60' : ''
                            )}>
                                {/* Show clean images in MediaGallery */}
                                {report.assets.filter(a => a.status === 'Clean' && a.downloadUrl && (a.contentType.startsWith('image/') || a.contentType.startsWith('video/'))).length > 0 && (
                                    <MediaGallery
                                        attachments={report.assets
                                            .filter(a => a.status === 'Clean' && a.downloadUrl && (a.contentType.startsWith('image/') || a.contentType.startsWith('video/')))
                                            .map(a => ({
                                                url: a.downloadUrl!,
                                                type: a.contentType,
                                                filename: a.fileName
                                            }))}
                                    />
                                )}

                                {/* Status list for all assets */}
                                <div className="space-y-2">
                                    {report.assets.map(asset => (
                                        <div key={asset.id} className="flex items-center gap-3 text-xs rounded-2xl border border-white/5 bg-muted/20 p-3 transition-colors hover:bg-muted/40">
                                            {asset.status === 'Pending' && (
                                                <Badge variant="outline" className="gap-1.5 text-amber-600 border-amber-500/20 bg-amber-500/5 text-[10px] px-2">
                                                    <Loader2 className="h-3 w-3 animate-spin" />
                                                    Scanning
                                                </Badge>
                                            )}
                                            {asset.status === 'Clean' && (
                                                <Badge variant="outline" className="gap-1.5 text-emerald-600 border-emerald-500/20 bg-emerald-500/5 text-[10px] px-2">
                                                    <ShieldCheck className="h-3 w-3" />
                                                    Clean
                                                </Badge>
                                            )}
                                            {asset.status === 'Infected' && (
                                                <Badge variant="destructive" className="gap-1.5 text-[10px] px-2">
                                                    <ShieldAlert className="h-3 w-3" />
                                                    Infected
                                                </Badge>
                                            )}
                                            {asset.status === 'Failed' && (
                                                <Badge variant="outline" className="gap-1.5 text-red-600 border-red-500/20 bg-red-500/5 text-[10px] px-2">
                                                    <AlertCircle className="h-3 w-3" />
                                                    Failed
                                                </Badge>
                                            )}
                                            <span className="truncate flex-1 font-medium">{asset.fileName}</span>
                                            <span className="text-muted-foreground text-[10px] tabular-nums">
                                                {(asset.sizeBytes / 1024).toFixed(1)} KB
                                            </span>
                                        </div>
                                    ))}
                                </div>
                            </CardContent>
                        </Card>
                    )}
                </div>
            </div>

            {/* Console Logs Section - Full Width */}
            {
                !report.isLocked && report.metadata && (() => {
                    try {
                        const metadata = JSON.parse(report.metadata);
                        if (metadata.logs) {
                            return <ConsoleLogsCard logs={metadata.logs} />;
                        }
                    } catch { return null; }
                    return null;
                })()
            }

            <AlertDialog open={confirmAction.isOpen} onOpenChange={(open: boolean) => {
                if (!open) setConfirmAction({ ...confirmAction, isOpen: false });
            }}>
                <AlertDialogContent>
                    <AlertDialogHeader>
                        <AlertDialogTitle>{confirmAction.title}</AlertDialogTitle>
                        <AlertDialogDescription>
                            {confirmAction.description}
                        </AlertDialogDescription>
                    </AlertDialogHeader>
                    <AlertDialogFooter>
                        <AlertDialogCancel>Cancel</AlertDialogCancel>
                        <AlertDialogAction onClick={confirmAction.action}>Confirm</AlertDialogAction>
                    </AlertDialogFooter>
                </AlertDialogContent>
            </AlertDialog>
        </div >
    );
}

