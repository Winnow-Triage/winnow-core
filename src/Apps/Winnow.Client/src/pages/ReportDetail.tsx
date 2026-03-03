import React, { useState } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { useParams, Link, useNavigate } from 'react-router-dom';
import { api } from '@/lib/api';
import { formatTimeAgo } from '@/lib/utils';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Separator } from '@/components/ui/separator';
import { ArrowLeft, ExternalLink, MessageSquare, Clock, AlertCircle, AlertTriangle, Sparkles, Paperclip, ShieldCheck, ShieldAlert, Loader2 } from 'lucide-react';
import { MediaGallery } from '@/components/MediaGallery';
import { ConsoleLogsCard } from '@/components/dashboard/ConsoleLogsCard';
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
                <div>
                    <div className="flex items-center gap-2">
                        <h1 className="text-2xl font-bold tracking-tight">R-{report.id.substring(0, 8)}</h1>
                        <Badge variant={report.status === 'Exported' ? 'default' : 'secondary'}>
                            {report.status}
                        </Badge>
                        {report.assignedTo && <Badge className="bg-blue-600">Assigned to: {report.assignedTo}</Badge>}
                    </div>
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
                <div className="bg-amber-100 dark:bg-amber-900/20 text-amber-800 dark:text-amber-200 border border-amber-200 dark:border-amber-800 rounded-lg p-4 flex items-center gap-3">
                    <AlertCircle className="h-5 w-5" />
                    <div className="flex-1">
                        <div className="flex items-center gap-2 mb-1">
                            <h4 className="font-semibold">This report belongs to a cluster</h4>
                            <Badge variant="outline" className={`bg-white/50 dark:bg-black/20 border-amber-300 dark:border-amber-700 ${report.confidenceScore && report.confidenceScore > 0.8 ? 'text-green-700 dark:text-green-400' : 'text-amber-700 dark:text-amber-400'}`}>
                                {report.confidenceScore !== undefined && report.confidenceScore !== null
                                    ? `${(report.confidenceScore * 100).toFixed(0)}% Match Confidence`
                                    : 'Confidence: N/A'}
                            </Badge>
                        </div>
                        <p className="text-sm opacity-90">
                            Cluster: <Link to={`/clusters/${report.clusterId}`} className="font-semibold underline">
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
                <div className="bg-red-50 dark:bg-red-900/20 text-red-800 dark:text-red-200 border border-red-200 dark:border-red-800 rounded-lg p-4 flex items-center gap-3 shadow-sm">
                    <ShieldAlert className="h-6 w-6 text-red-600 dark:text-red-400 shrink-0" />
                    <div className="flex-1">
                        <h4 className="font-bold text-lg">Report Locked</h4>
                        <p className="text-sm opacity-90 mt-1">
                            Your organization has exceeded its monthly ingestion limit. Upgrade to a higher tier to view this stack trace and unlock your remaining reports.
                        </p>
                    </div>
                    <Button asChild className="shrink-0 bg-red-600 hover:bg-red-700 text-white">
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
                <div className="bg-blue-50 dark:bg-blue-900/20 text-blue-800 dark:text-blue-200 border border-blue-200 dark:border-blue-800 rounded-lg p-4 flex items-center gap-3">
                    <Sparkles className="h-5 w-5 text-blue-600 dark:text-blue-400" />
                    <div className="flex-1">
                        <div className="flex items-center gap-2 mb-1">
                            <h4 className="font-semibold">Suggested Cluster Match</h4>
                            <Badge variant="outline" className="bg-white/50 dark:bg-black/20 border-blue-300 dark:border-blue-700 text-blue-700 dark:text-blue-400">
                                {report.suggestedConfidenceScore !== undefined && report.suggestedConfidenceScore !== null
                                    ? `${(report.suggestedConfidenceScore * 100).toFixed(0)}% Similarity`
                                    : 'Similarity: N/A'}
                            </Badge>
                        </div>
                        <p className="text-sm opacity-90">
                            This report looks very similar to cluster: <span className="font-medium">{report.suggestedClusterSummary || report.suggestedClusterId}</span>.
                        </p>
                    </div>
                    <div className="flex gap-2">
                        <Button
                            variant="default"
                            size="sm"
                            className="bg-blue-600 hover:bg-blue-700 text-white"
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
                            className="text-blue-800 dark:text-blue-200 hover:bg-blue-100 dark:hover:bg-blue-800/50"
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
                        <h1 className="text-3xl font-bold text-gray-900 dark:text-white leading-tight">
                            {report.title}
                        </h1>
                        <div className="flex items-center gap-2 text-sm text-gray-500 dark:text-gray-400">
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
                        <div className="flex items-center gap-2 p-4 bg-purple-50 dark:bg-purple-900/10 border border-purple-100 dark:border-purple-800 rounded-lg">
                            <Sparkles className="h-5 w-5 text-purple-600 dark:text-purple-400" />
                            <div className="flex-1 text-sm">
                                <span className="font-semibold text-purple-900 dark:text-purple-300">AI Analysis Available:</span> This report is part of a cluster with an AI-generated summary and criticality analysis.
                            </div>
                            <Button variant="outline" size="sm" asChild className="border-purple-200 dark:border-purple-700 text-purple-700 dark:text-purple-300">
                                <Link to={`/clusters/${report.clusterId}`}>
                                    View Cluster Analysis
                                </Link>
                            </Button>
                        </div>
                    )}



                    <Card>
                        <CardHeader>
                            <CardTitle className="text-lg">User Description</CardTitle>
                        </CardHeader>
                        <CardContent>
                            <div className={`prose dark:prose-invert max-w-none ${report.isLocked ? 'blur-md select-none pointer-events-none opacity-60' : ''}`}>
                                <p className="whitespace-pre-wrap">{report.isLocked ? 'This content is hidden. Please upgrade your plan to unlock this data.' : report.message}</p>
                            </div>
                        </CardContent>
                    </Card>

                    <Card>
                        <CardHeader>
                            <CardTitle className="text-lg">Activity Log</CardTitle>
                        </CardHeader>
                        <CardContent>
                            <div className="text-sm text-muted-foreground">
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
                        <Card>
                            <CardHeader>
                                <CardTitle className="text-sm font-medium flex items-center gap-2">
                                    <Paperclip className="h-3 w-3" />
                                    Attachments
                                    <Badge variant="outline" className="ml-auto text-xs">
                                        {report.assets.length} file{report.assets.length > 1 ? 's' : ''}
                                    </Badge>
                                </CardTitle>
                                <CardDescription>Files captured with this report.</CardDescription>
                            </CardHeader>
                            <CardContent className={`space-y-3 ${report.isLocked ? 'blur-md select-none pointer-events-none opacity-60' : ''}`}>
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
                                        <div key={asset.id} className="flex items-center gap-2 text-sm rounded-md border p-2">
                                            {asset.status === 'Pending' && (
                                                <Badge variant="outline" className="gap-1 text-yellow-600 border-yellow-300">
                                                    <Loader2 className="h-3 w-3 animate-spin" />
                                                    Scanning
                                                </Badge>
                                            )}
                                            {asset.status === 'Clean' && (
                                                <Badge variant="outline" className="gap-1 text-green-600 border-green-300">
                                                    <ShieldCheck className="h-3 w-3" />
                                                    Clean
                                                </Badge>
                                            )}
                                            {asset.status === 'Infected' && (
                                                <Badge variant="destructive" className="gap-1">
                                                    <ShieldAlert className="h-3 w-3" />
                                                    Infected
                                                </Badge>
                                            )}
                                            {asset.status === 'Failed' && (
                                                <Badge variant="outline" className="gap-1 text-red-600 border-red-300">
                                                    <AlertCircle className="h-3 w-3" />
                                                    Failed
                                                </Badge>
                                            )}
                                            <span className="truncate flex-1">{asset.fileName}</span>
                                            <span className="text-muted-foreground text-xs">
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

