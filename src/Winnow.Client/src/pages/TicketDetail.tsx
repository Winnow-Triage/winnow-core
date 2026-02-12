import { useQuery, useQueryClient } from '@tanstack/react-query';
import { useParams, Link, useNavigate } from 'react-router-dom';
import { api } from '@/lib/api';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Separator } from '@/components/ui/separator';
import { ArrowLeft, ExternalLink, MessageSquare, Clock, AlertCircle, Sparkles } from 'lucide-react';
import { Input } from '@/components/ui/input';
import { useState } from 'react';
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

interface RelatedTicket {
    id: string;
    title: string;
    status: string;
    createdAt: string;
}

interface TicketDetailData {
    id: string;
    title: string;
    description: string;
    status: string;
    createdAt: string;
    parentTicketId?: string;
    parentTicketTitle?: string;
    assignedTo?: string;
    summary?: string;
    evidence: RelatedTicket[];
}

export default function TicketDetail() {
    const { id } = useParams<{ id: string }>();
    const navigate = useNavigate();
    const queryClient = useQueryClient();
    const [assignee, setAssignee] = useState('');
    const [isGeneratingSummary, setIsGeneratingSummary] = useState(false);
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
                        <h4 className="font-semibold">This ticket is a duplicate</h4>
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

            <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
                {/* Main Content: Master Description */}
                <div className="md:col-span-2 flex flex-col gap-6">
                    {/* AI Summary Section - Only for Clusters (Parents with children) */}
                    {ticket.evidence.length > 0 && (
                        <Card className="border-purple-200 dark:border-purple-800 bg-purple-50/50 dark:bg-purple-900/10">
                            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                                <CardTitle className="text-lg font-semibold flex items-center gap-2 text-purple-900 dark:text-purple-100">
                                    <Sparkles className="h-5 w-5 text-purple-600 dark:text-purple-400" />
                                    AI Cluster Summary
                                </CardTitle>
                                {!ticket.summary && (
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
                                            } catch (e) {
                                                console.error("Failed to generate summary", e);
                                            } finally {
                                                setIsGeneratingSummary(false);
                                            }
                                        }}
                                    >
                                        {isGeneratingSummary ? 'Generating...' : 'Generate AI Summary'}
                                    </Button>
                                )}
                            </CardHeader>
                            <CardContent>
                                {ticket.summary ? (
                                    <div className="prose dark:prose-invert max-w-none text-purple-900 dark:text-purple-100">
                                        <p className="whitespace-pre-wrap italic">{ticket.summary}</p>
                                    </div>
                                ) : (
                                    <div className="text-sm text-muted-foreground italic">
                                        No AI summary generated yet. Click the button to analyze this cluster.
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
                                <span>{new Date(ticket.createdAt).toLocaleString()}</span>
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
                                                <div className="font-medium text-sm truncate">{child.title}</div>
                                                <div className="text-xs text-muted-foreground flex justify-between mt-1">
                                                    <span>{new Date(child.createdAt).toLocaleDateString()}</span>
                                                    <Badge variant="outline" className="text-[10px] h-4 px-1">{child.status}</Badge>
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
