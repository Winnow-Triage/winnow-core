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
}

export default function Clusters() {
    const [search, setSearch] = useState('');

    const { data: tickets, isLoading } = useQuery<Ticket[]>({
        queryKey: ['tickets'],
        queryFn: async () => {
            const { data } = await api.get('/tickets');
            return data;
        },
    });

    // A "Cluster" is a Parent Ticket (no parentTicketId) or a ticket that has become a parent.
    // For now, based on current logic, ParentTicketId == null means it's a potential cluster head.
    // In our system, every ticket is a cluster of size 1 unless it has children or is a child.
    // To be useful, let's show tickets that ARE parents.

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

    // Sort by cluster size (interesting ones first)
    const sortedClusters = [...clusters].sort((a, b) => {
        const countA = clusterMap.get(a.id) || 0;
        const countB = clusterMap.get(b.id) || 0;
        return countB - countA;
    });

    return (
        <div className="flex flex-col gap-6">
            <div className="flex items-center justify-between">
                <div className="flex items-center gap-2">
                    <LayoutDashboard className="h-6 w-6 text-muted-foreground" />
                    <h1 className="text-3xl font-bold tracking-tight">Active Clusters</h1>
                </div>
                <div className="w-1/3">
                    <Input
                        placeholder="Search clusters..."
                        value={search}
                        onChange={(e) => setSearch(e.target.value)}
                    />
                </div>
            </div>

            <div className="border rounded-md">
                <Table>
                    <TableHeader>
                        <TableRow>
                            <TableHead>Cluster Title</TableHead>
                            <TableHead>Status</TableHead>
                            <TableHead>Created</TableHead>
                            <TableHead className="text-right">Related Tickets</TableHead>
                        </TableRow>
                    </TableHeader>
                    <TableBody>
                        {isLoading ? (
                            <TableRow>
                                <TableCell colSpan={4} className="h-24 text-center">Loading...</TableCell>
                            </TableRow>
                        ) : sortedClusters.length === 0 ? (
                            <TableRow>
                                <TableCell colSpan={4} className="h-24 text-center">No clusters found.</TableCell>
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
