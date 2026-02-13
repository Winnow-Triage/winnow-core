import React, { useState } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import ReactMarkdown from 'react-markdown';
import { useParams, Link, useNavigate } from 'react-router-dom';
import { api } from '@/lib/api';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Separator } from '@/components/ui/separator';
import { ArrowLeft, ExternalLink, MessageSquare, Clock, AlertCircle, Sparkles } from 'lucide-react';
import { Input } from '@/components/ui/input';
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
import {
    DropdownMenu,
    DropdownMenuContent,
    DropdownMenuItem,
    DropdownMenuSeparator,
    DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { MoreHorizontal, RotateCw, Trash2 } from 'lucide-react';

interface RelatedTicket {
    id: string;
    title: string;
    status: string;
    createdAt: string;
    confidenceScore?: number;
}

interface TicketDetailData {
    id: string;
    title: string;
    description: string;
    status: string;
    createdAt: string;
    parentTicketId?: string;
    parentTicketTitle?: string;
    suggestedParentId?: string;
    suggestedConfidenceScore?: number;
    suggestedParentTitle?: string;
    assignedTo?: string;
    summary?: string;
    confidenceScore?: number;
    criticalityScore?: number;
    criticalityReasoning?: string;
    metadataJson?: string;
    evidence: RelatedTicket[];
}

export default function TicketDetail() {
    const { id } = useParams<{ id: string }>();
    const navigate = useNavigate();
    const queryClient = useQueryClient();
    const [assignee, setAssignee] = useState('');
    const [isGeneratingSummary, setIsGeneratingSummary] = useState(false);
    const [isClearingSummary, setIsClearingSummary] = useState(false);
    const [confirmAction, setConfirmAction] = useState<{
        isOpen: boolean;
        title: string;
        description: string;
        action: () => Promise<void>;
    }>({ isOpen: false, title: '', description: '', action: async () => { } });

    const { data: ticket, isLoading, error } = useQuery<TicketDetailData>({
        queryKey: ['ticket', id],
        queryFn: async () => {
            const { data } = await api.get(`/tickets/${id}`);
            return data;
        },
        enabled: !!id,
    });

    if (isLoading) return <div className="p-8">Loading ticket details...</div>;
    if (error || !ticket) return <div className="p-8 text-red-500">Error loading ticket.</div>;

    return (
        <div className="flex flex-col gap-6 max-w-5xl mx-auto w-full">
            {/* Header / Navigation */}
            <div className="flex items-center gap-4">
                <Button variant="ghost" size="icon" onClick={() => navigate(-1)}>
                    <ArrowLeft className="h-4 w-4" />
                </Button>
                <div>
                    <div className="flex items-center gap-2">
                        <h1 className="text-2xl font-bold tracking-tight">T-{ticket.id.substring(0, 8)}</h1>
                        <Badge variant={ticket.status === 'Exported' ? 'default' : 'secondary'}>
                            {ticket.status}
                        </Badge>
                        {ticket.assignedTo && <Badge className="bg-blue-600">Assigned to: {ticket.assignedTo}</Badge>}
                    </div>
                </div>
                <div className="ml-auto flex items-center gap-2">
                    <div className="flex w-full max-w-sm items-center space-x-2">
                        <Input
                            placeholder={ticket.assignedTo ? "Reassign..." : "Assign to..."}
                            value={assignee}
                            onChange={(e) => setAssignee(e.target.value)}
                            className="w-32 h-8"
                        />
                        <Button
                            className="h-8"
                            disabled={!assignee}
                            onClick={async () => {
                                await api.post(`/tickets/${ticket.id}/assign`, { assignedTo: assignee });
                                queryClient.invalidateQueries({ queryKey: ['ticket', id] });
                                queryClient.invalidateQueries({ queryKey: ['tickets'] });
                            }}
                        >
                            Assign
                        </Button>
                        {ticket.assignedTo && (
                            <Button
                                variant="ghost"
                                size="sm"
                                className="h-8 text-red-500 hover:text-red-700"
                                onClick={() => {
                                    setConfirmAction({
                                        isOpen: true,
                                        title: 'Unassign Ticket?',
                                        description: 'Are you sure you want to unassign this ticket?',
                                        action: async () => {
                                            await api.post(`/tickets/${ticket.id}/assign`, { assignedTo: null });
                                            queryClient.invalidateQueries({ queryKey: ['ticket', id] });
                                            queryClient.invalidateQueries({ queryKey: ['tickets'] });
                                        }
                                    });
                                }}
                            >
                                Unassign
                            </Button>
                        )}
                    </div>
                    <Button variant="outline" onClick={() => {
                        setConfirmAction({
                            isOpen: true,
                            title: 'Close Cluster?',
                            description: 'Are you sure you want to CLOSE ALL tickets in this cluster? This action cannot be easily undone.',
                            action: async () => {
                                await api.post(`/tickets/${ticket.id}/close-cluster`, {});
                                queryClient.invalidateQueries({ queryKey: ['ticket', id] });
                                queryClient.invalidateQueries({ queryKey: ['tickets'] });
                            }
                        });
                    }}>
                        Close Cluster
                    </Button>
                    <Button variant="outline">
                        <ExternalLink className="mr-2 h-4 w-4" />
                        Open in External System
                    </Button>
                    <Button>
                        Export to Trello
                    </Button>
                </div>
            </div>

            {/* Duplicate Alert */}
            {ticket.parentTicketId && (
                <div className="bg-amber-100 dark:bg-amber-900/20 text-amber-800 dark:text-amber-200 border border-amber-200 dark:border-amber-800 rounded-lg p-4 flex items-center gap-3">
                    <AlertCircle className="h-5 w-5" />
                    <div className="flex-1">
                        <div className="flex items-center gap-2 mb-1">
                            <h4 className="font-semibold">This ticket is a duplicate</h4>
                            <Badge variant="outline" className={`bg-white/50 dark:bg-black/20 border-amber-300 dark:border-amber-700 ${ticket.confidenceScore && ticket.confidenceScore > 0.8 ? 'text-green-700 dark:text-green-400' : 'text-amber-700 dark:text-amber-400'}`}>
                                {ticket.confidenceScore !== undefined && ticket.confidenceScore !== null
                                    ? `${(ticket.confidenceScore * 100).toFixed(0)}% Match Confidence`
                                    : 'Confidence: N/A'}
                            </Badge>
                        </div>
                        <p className="text-sm opacity-90">
                            It has been merged into <Link to={`/tickets/${ticket.parentTicketId}`} className="underline font-medium break-all">{ticket.parentTicketTitle || ticket.parentTicketId}</Link>.
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
                                    title: 'Ungroup Ticket?',
                                    description: 'Are you sure you want to ungroup this ticket? It will be treated as specific unique issue.',
                                    action: async () => {
                                        await api.post(`/tickets/${ticket.id}/ungroup`, {});
                                        queryClient.invalidateQueries({ queryKey: ['ticket', id] });
                                        queryClient.invalidateQueries({ queryKey: ['tickets'] });
                                    }
                                });
                            }}
                        >
                            Ungroup
                        </Button>
                        <Button variant="ghost" size="sm" asChild className="shrink-0 hover:bg-amber-200 dark:hover:bg-amber-800/50">
                            <Link to={`/tickets/${ticket.parentTicketId}`}>
                                View Original Correct Ticket
                            </Link>
                        </Button>
                    </div>
                </div>
            )}

            {/* Suggested Match Alert */}
            {!ticket.parentTicketId && ticket.suggestedParentId && (
                <div className="bg-blue-50 dark:bg-blue-900/20 text-blue-800 dark:text-blue-200 border border-blue-200 dark:border-blue-800 rounded-lg p-4 flex items-center gap-3">
                    <Sparkles className="h-5 w-5 text-blue-600 dark:text-blue-400" />
                    <div className="flex-1">
                        <div className="flex items-center gap-2 mb-1">
                            <h4 className="font-semibold">Suggested Match Found</h4>
                            <Badge variant="outline" className="bg-white/50 dark:bg-black/20 border-blue-300 dark:border-blue-700 text-blue-700 dark:text-blue-400">
                                {ticket.suggestedConfidenceScore !== undefined && ticket.suggestedConfidenceScore !== null
                                    ? `${(ticket.suggestedConfidenceScore * 100).toFixed(0)}% Similarity`
                                    : 'Similarity: N/A'}
                            </Badge>
                        </div>
                        <p className="text-sm opacity-90">
                            This ticket looks very similar to <Link to={`/tickets/${ticket.suggestedParentId}`} className="underline font-medium break-all">{ticket.suggestedParentTitle || ticket.suggestedParentId}</Link>.
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
                                    description: `Are you sure you want to merge this ticket into "${ticket.suggestedParentTitle || 'the suggested parent'}"?`,
                                    action: async () => {
                                        await api.post(`/tickets/${ticket.id}/accept-suggestion`, {});
                                        queryClient.invalidateQueries({ queryKey: ['ticket', id] });
                                        queryClient.invalidateQueries({ queryKey: ['tickets'] });
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
                                    await api.post(`/tickets/${ticket.id}/dismiss-suggestion`, {});
                                    queryClient.invalidateQueries({ queryKey: ['ticket', id] });
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
                    {/* AI Summary Section - Show if summary exists or if it's a cluster parent */}
                    {(ticket.summary || ticket.criticalityScore || ticket.evidence.length > 0) && (
                        <Card className="border-purple-500/30 bg-[#160d33] dark:bg-[#160d33] shadow-2xl shadow-purple-500/10 relative overflow-hidden group transition-all duration-300">
                            {/* Inner Glow Border Overlay */}
                            <div className="absolute inset-0 z-0 pointer-events-none border border-transparent bg-gradient-to-br from-purple-500/10 via-transparent to-indigo-500/10 rounded-xl transition-opacity opacity-20 group-hover:opacity-100" />

                            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2 relative z-10">
                                <div className="flex flex-col gap-1">
                                    <CardTitle className="text-xl font-bold flex items-center gap-2 tracking-tight text-white dark:text-white">
                                        <Sparkles className="h-5 w-5 text-purple-400 fill-purple-500/20 animate-spin-slow" />
                                        AI Perspective
                                    </CardTitle>
                                    {ticket.criticalityScore && (
                                        <div className="flex items-center gap-2">
                                            <Badge variant="outline" className={`
                                                ${ticket.criticalityScore >= 8 ? 'bg-red-100 text-red-800 border-red-200 dark:bg-red-900/30 dark:text-red-300 dark:border-red-800' :
                                                    ticket.criticalityScore >= 5 ? 'bg-amber-100 text-amber-800 border-amber-200 dark:bg-amber-900/30 dark:text-amber-300 dark:border-amber-800' :
                                                        'bg-blue-100 text-blue-800 border-blue-200 dark:bg-blue-900/30 dark:text-blue-300 dark:border-blue-800'}
                                            `}>
                                                Criticality: {ticket.criticalityScore}/10
                                            </Badge>
                                            {ticket.criticalityReasoning && (
                                                <span className="text-xs text-muted-foreground" title={ticket.criticalityReasoning}>
                                                    {ticket.criticalityReasoning.length > 60
                                                        ? ticket.criticalityReasoning.substring(0, 60) + '...'
                                                        : ticket.criticalityReasoning}
                                                </span>
                                            )}
                                        </div>
                                    )}
                                </div>
                                {!ticket.summary ? (
                                    <Button
                                        size="sm"
                                        variant="outline"
                                        className="h-8 border-purple-200 dark:border-purple-700 hover:bg-purple-100 dark:hover:bg-purple-800 text-purple-700 dark:text-purple-300"
                                        disabled={isGeneratingSummary}
                                        onClick={async () => {
                                            setIsGeneratingSummary(true);
                                            try {
                                                await api.post(`/tickets/${ticket.id}/generate-summary`, {});
                                                queryClient.invalidateQueries({ queryKey: ['ticket', id] });
                                                queryClient.invalidateQueries({ queryKey: ['tickets'] });
                                            } catch (e) {
                                                console.error("Failed to generate summary", e);
                                            } finally {
                                                setIsGeneratingSummary(false);
                                            }
                                        }}
                                    >
                                        {isGeneratingSummary ? (
                                            <>
                                                <Sparkles className="mr-2 h-3 w-3 animate-spin" />
                                                Generating...
                                            </>
                                        ) : (
                                            'Analyze with AI'
                                        )}
                                    </Button>
                                ) : (
                                    <DropdownMenu>
                                        <DropdownMenuTrigger asChild>
                                            <Button variant="ghost" size="icon" className="h-8 w-8 text-purple-700 dark:text-purple-300">
                                                <MoreHorizontal className="h-4 w-4" />
                                            </Button>
                                        </DropdownMenuTrigger>
                                        <DropdownMenuContent align="end">
                                            <DropdownMenuItem
                                                disabled={isGeneratingSummary || isClearingSummary}
                                                onClick={async () => {
                                                    setIsGeneratingSummary(true);
                                                    try {
                                                        await api.post(`/tickets/${ticket.id}/generate-summary`, {});
                                                        await queryClient.invalidateQueries({ queryKey: ['ticket', id] });
                                                        await queryClient.invalidateQueries({ queryKey: ['tickets'] });
                                                    } catch (e) {
                                                        console.error("Failed to regenerate summary", e);
                                                    } finally {
                                                        setIsGeneratingSummary(false);
                                                    }
                                                }}
                                            >
                                                <RotateCw className={`mr-2 h-4 w-4 ${isGeneratingSummary ? 'animate-spin' : ''}`} />
                                                Regenerate Summary
                                            </DropdownMenuItem>
                                            <DropdownMenuSeparator />
                                            <DropdownMenuItem
                                                className="text-red-600 focus:text-red-600"
                                                disabled={isGeneratingSummary || isClearingSummary}
                                                onClick={async () => {
                                                    setIsClearingSummary(true);
                                                    try {
                                                        await api.post(`/tickets/${ticket.id}/clear-summary`, {});
                                                        await queryClient.invalidateQueries({ queryKey: ['ticket', id] });
                                                    } catch (e) {
                                                        console.error("Failed to clear summary", e);
                                                    } finally {
                                                        setIsClearingSummary(false);
                                                    }
                                                }}
                                            >
                                                {isClearingSummary ? (
                                                    <RotateCw className="mr-2 h-4 w-4 animate-spin" />
                                                ) : (
                                                    <Trash2 className="mr-2 h-4 w-4" />
                                                )}
                                                Clear Summary
                                            </DropdownMenuItem>
                                        </DropdownMenuContent>
                                    </DropdownMenu>
                                )}
                            </CardHeader>
                            <CardContent className="relative min-h-[120px] z-10 pt-4">
                                {isGeneratingSummary && ticket.summary && (
                                    <div className="absolute inset-0 bg-[#0f0a1f] flex items-center justify-center z-20 rounded-b-xl transition-all duration-300">
                                        <div className="flex flex-col items-center gap-3 text-white animate-in fade-in zoom-in duration-500">
                                            <div className="relative">
                                                <Sparkles className="h-8 w-8 text-purple-400 animate-spin-slow" />
                                                <RotateCw className="h-8 w-8 absolute inset-0 text-purple-400 animate-spin opacity-40" />
                                            </div>
                                            <span className="font-bold text-sm tracking-widest uppercase text-purple-200">Analyzing Data...</span>
                                        </div>
                                    </div>
                                )}
                                {ticket.summary ? (
                                    <div className="prose prose-invert max-w-none 
                                        text-purple-50 
                                        prose-headings:text-purple-100 
                                        prose-strong:text-white 
                                        relative z-10">
                                        <ReactMarkdown>{ticket.summary}</ReactMarkdown>
                                    </div>
                                ) : (
                                    <div className="flex flex-col items-center justify-center py-8 gap-4 text-center">
                                        <div className="p-4 bg-purple-900/30 rounded-full animate-pulse">
                                            <Sparkles className="h-8 w-8 text-purple-400" />
                                        </div>
                                        <div className="space-y-1">
                                            <p className="text-sm font-semibold text-purple-200">No AI Insight Available</p>
                                            <p className="text-xs text-muted-foreground max-w-[240px]">This ticket has not been analyzed yet. Run the AI perspective to generate a cluster-wide summary.</p>
                                        </div>
                                    </div>
                                )}
                            </CardContent>
                        </Card>
                    )}

                    <Card>
                        <CardHeader>
                            <CardTitle className="text-xl">Master Description</CardTitle>
                            <CardDescription>
                                Consolidated view of the issue.
                            </CardDescription>
                        </CardHeader>
                        <CardContent>
                            <div className="prose dark:prose-invert max-w-none">
                                <p className="whitespace-pre-wrap">{ticket.description}</p>
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
                                <span className="text-right">{new Date(ticket.createdAt).toLocaleString()}</span>
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
                            {ticket.metadataJson && (
                                <>
                                    <Separator />
                                    <div className="flex flex-col gap-2">
                                        <span className="text-muted-foreground flex items-center gap-2">
                                            <Sparkles className="h-3 w-3" /> Game Metadata
                                        </span>
                                        <div className="bg-muted/50 p-2 rounded-md grid grid-cols-2 gap-x-4 gap-y-1">
                                            {(() => {
                                                try {
                                                    const metadata = JSON.parse(ticket.metadataJson);
                                                    return Object.entries(metadata).map(([key, value]) => (
                                                        <React.Fragment key={key}>
                                                            <span className="text-xs font-medium text-muted-foreground uppercase">{key}</span>
                                                            <span className="text-xs text-right font-mono">{String(value)}</span>
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

                    <Card>
                        <CardHeader>
                            <CardTitle className="text-sm font-medium">Evidence Locker ({ticket.evidence.length})</CardTitle>
                            <CardDescription>Related tickets in this cluster.</CardDescription>
                        </CardHeader>
                        <CardContent>
                            {ticket.evidence.length === 0 ? (
                                <div className="text-sm text-muted-foreground">No related tickets found.</div>
                            ) : (
                                <ul className="flex flex-col gap-2">
                                    {ticket.evidence.map(child => (
                                        <li key={child.id} className="p-2 border rounded-md hover:bg-muted/50 transition-colors">
                                            <Link to={`/tickets/${child.id}`} className="block">
                                                <div className="flex items-center justify-between gap-2">
                                                    <div className="font-medium text-sm truncate">{child.title}</div>
                                                    {child.confidenceScore !== undefined && child.confidenceScore !== null && (
                                                        <span className="text-[10px] font-semibold text-blue-600 dark:text-blue-400 whitespace-nowrap">
                                                            {(child.confidenceScore * 100).toFixed(0)}% Sim
                                                        </span>
                                                    )}
                                                </div>
                                                <div className="text-xs text-muted-foreground flex justify-between mt-1">
                                                    <span>{new Date(child.createdAt).toLocaleDateString()}</span>
                                                    <div className="flex gap-1">
                                                        <Badge variant="outline" className="text-[10px] h-4 px-1">{child.status}</Badge>
                                                    </div>
                                                </div>
                                            </Link>
                                        </li>
                                    ))}
                                </ul>
                            )}
                        </CardContent>
                    </Card>
                </div>
            </div>

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
        </div>
    );
}
