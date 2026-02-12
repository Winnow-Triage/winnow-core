import { useQuery } from '@tanstack/react-query';
import { api } from '@/lib/api';
import {
    Table,
    TableBody,
    TableCell,
    TableHead,
    TableHeader,
    TableRow,
} from '@/components/ui/table';
import { Badge } from '@/components/ui/badge';
import { Link } from 'react-router-dom';
import { Input } from '@/components/ui/input';
import { useState } from 'react';
import { LayoutDashboard } from 'lucide-react';

interface Ticket {
    id: string;
    title: string;
    status: string;
    createdAt: string;
    parentTicketId?: string;
    criticalityScore?: number;
}

export default function Clusters() {
    const [search, setSearch] = useState('');
    const [sortBy, setSortBy] = useState<'size' | 'criticality' | 'newest'>('size');

    const { data: tickets, isLoading } = useQuery<Ticket[]>({
        queryKey: ['tickets'],
        queryFn: async () => {
            const { data } = await api.get('/tickets');
            return data;
        },
        staleTime: 60 * 1000,
    });

    // We need to count children to be useful.
    const clusterMap = new Map<string, number>();
    tickets?.forEach(t => {
        if (t.parentTicketId) {
            clusterMap.set(t.parentTicketId, (clusterMap.get(t.parentTicketId) || 0) + 1);
        }
    });

    const clusters = tickets?.filter(t => !t.parentTicketId && (
        t.title.toLowerCase().includes(search.toLowerCase())
    )) || [];

    // Sort based on selected metric
    const sortedClusters = [...clusters].sort((a, b) => {
        if (sortBy === 'size') {
            const countA = clusterMap.get(a.id) || 0;
            const countB = clusterMap.get(b.id) || 0;
            return countB - countA;
        }
        if (sortBy === 'criticality') {
            return (b.criticalityScore || 0) - (a.criticalityScore || 0);
        }
        return new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime();
    });

    return (
        <div className="flex flex-col gap-6">
            <div className="flex items-center justify-between">
                <div className="flex items-center gap-2">
                    <LayoutDashboard className="h-6 w-6 text-muted-foreground" />
                    <h1 className="text-3xl font-bold tracking-tight">Active Clusters</h1>
                </div>
                <div className="flex items-center gap-4 w-1/2 justify-end">
                    <div className="flex items-center gap-2">
                        <span className="text-sm text-muted-foreground whitespace-nowrap">Sort by:</span>
                        <select
                            className="bg-background border rounded px-2 py-1 text-sm outline-none focus:ring-1 focus:ring-ring"
                            value={sortBy}
                            onChange={(e) => setSortBy(e.target.value as any)}
                        >
                            <option value="size">Cluster Size</option>
                            <option value="criticality">Criticality</option>
                            <option value="newest">Newest</option>
                        </select>
                    </div>
                    <div className="w-1/2">
                        <Input
                            placeholder="Search clusters..."
                            value={search}
                            onChange={(e) => setSearch(e.target.value)}
                        />
                    </div>
                </div>
            </div>

            <div className="border rounded-md">
                <Table>
                    <TableHeader>
                        <TableRow>
                            <TableHead>Cluster Title</TableHead>
                            <TableHead>Status</TableHead>
                            <TableHead>Criticality</TableHead>
                            <TableHead>Created</TableHead>
                            <TableHead className="text-right">Related Tickets</TableHead>
                        </TableRow>
                    </TableHeader>
                    <TableBody>
                        {isLoading ? (
                            <TableRow>
                                <TableCell colSpan={5} className="h-24 text-center">Loading...</TableCell>
                            </TableRow>
                        ) : sortedClusters.length === 0 ? (
                            <TableRow>
                                <TableCell colSpan={5} className="h-24 text-center">No clusters found.</TableCell>
                            </TableRow>
                        ) : (
                            sortedClusters.map((ticket) => {
                                const childCount = clusterMap.get(ticket.id) || 0;
                                return (
                                    <TableRow key={ticket.id}>
                                        <TableCell className="font-medium">
                                            <Link to={`/tickets/${ticket.id}`} className="hover:underline block font-semibold">
                                                {ticket.title}
                                            </Link>
                                        </TableCell>
                                        <TableCell>
                                            <Badge variant="outline">{ticket.status}</Badge>
                                        </TableCell>
                                        <TableCell>
                                            {ticket.criticalityScore ? (
                                                <Badge variant="outline" className={`
                                                    ${ticket.criticalityScore >= 8 ? 'bg-red-100 text-red-800 border-red-200 dark:bg-red-900/30' :
                                                        ticket.criticalityScore >= 5 ? 'bg-amber-100 text-amber-800 border-amber-200 dark:bg-amber-900/30' :
                                                            'bg-blue-100 text-blue-800 border-blue-200 dark:bg-blue-900/30'}
                                                `}>
                                                    {ticket.criticalityScore}/10
                                                </Badge>
                                            ) : (
                                                <span className="text-xs text-muted-foreground italic">Pending...</span>
                                            )}
                                        </TableCell>
                                        <TableCell>{new Date(ticket.createdAt).toLocaleDateString()}</TableCell>
                                        <TableCell className="text-right">
                                            <Badge variant={childCount > 0 ? "default" : "secondary"}>
                                                {childCount + 1} {/* +1 for the parent itself */}
                                            </Badge>
                                        </TableCell>
                                    </TableRow>
                                );
                            })
                        )}
                    </TableBody>
                </Table>
            </div>
        </div>
    );
}
