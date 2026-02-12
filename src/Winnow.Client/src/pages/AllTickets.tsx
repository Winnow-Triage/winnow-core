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

interface Ticket {
    id: string;
    title: string;
    status: string;
    createdAt: string;
    parentTicketId?: string;
    confidenceScore?: number;
}

export default function AllTickets() {
    const [search, setSearch] = useState('');
    const [sortConfig, setSortConfig] = useState<{ key: keyof Ticket; direction: 'asc' | 'desc' } | null>(null);

    const { data: tickets, isLoading } = useQuery<Ticket[]>({
        queryKey: ['tickets'],
        queryFn: async () => {
            const { data } = await api.get('/tickets');
            return data;
        },
    });

    const filteredTickets = tickets?.filter(t =>
        t.title.toLowerCase().includes(search.toLowerCase()) ||
        t.status.toLowerCase().includes(search.toLowerCase())
    ) || [];

    const sortedTickets = [...filteredTickets].sort((a, b) => {
        if (!sortConfig) return 0;
        const { key, direction } = sortConfig;

        const aValue = a[key] ?? ((key === 'confidenceScore') ? 0 : '');
        const bValue = b[key] ?? ((key === 'confidenceScore') ? 0 : '');

        if (aValue < bValue) return direction === 'asc' ? -1 : 1;
        if (aValue > bValue) return direction === 'asc' ? 1 : -1;
        return 0;
    });

    const handleSort = (key: keyof Ticket) => {
        let direction: 'asc' | 'desc' = 'asc';
        if (sortConfig && sortConfig.key === key && sortConfig.direction === 'asc') {
            direction = 'desc';
        }
        setSortConfig({ key, direction });
    };

    return (
        <div className="flex flex-col gap-6">
            <div className="flex items-center justify-between">
                <h1 className="text-3xl font-bold tracking-tight">All Tickets</h1>
                <div className="w-1/3">
                    <Input
                        placeholder="Search tickets..."
                        value={search}
                        onChange={(e) => setSearch(e.target.value)}
                    />
                </div>
            </div>

            <div className="border rounded-md">
                <Table>
                    <TableHeader>
                        <TableRow>
                            <TableHead className="cursor-pointer" onClick={() => handleSort('title')}>Title</TableHead>
                            <TableHead className="cursor-pointer" onClick={() => handleSort('status')}>Status</TableHead>
                            <TableHead className="cursor-pointer" onClick={() => handleSort('createdAt')}>Created</TableHead>
                            <TableHead>Cluster</TableHead>
                            <TableHead className="cursor-pointer" onClick={() => handleSort('confidenceScore')}>Confidence</TableHead>
                        </TableRow>
                    </TableHeader>
                    <TableBody>
                        {isLoading ? (
                            <TableRow>
                                <TableCell colSpan={5} className="h-24 text-center">Loading...</TableCell>
                            </TableRow>
                        ) : sortedTickets.length === 0 ? (
                            <TableRow>
                                <TableCell colSpan={5} className="h-24 text-center">No tickets found.</TableCell>
                            </TableRow>
                        ) : (
                            sortedTickets.map((ticket) => (
                                <TableRow key={ticket.id}>
                                    <TableCell className="font-medium">
                                        <Link to={`/tickets/${ticket.id}`} className="hover:underline block">
                                            {ticket.title}
                                        </Link>
                                    </TableCell>
                                    <TableCell>
                                        <Badge variant="outline">{ticket.status}</Badge>
                                    </TableCell>
                                    <TableCell>{new Date(ticket.createdAt).toLocaleDateString()}</TableCell>
                                    <TableCell>
                                        {ticket.parentTicketId ? (
                                            <Link to={`/tickets/${ticket.parentTicketId}`} className="text-xs text-muted-foreground hover:underline">
                                                View Parent
                                            </Link>
                                        ) : (
                                            <span className="text-xs text-muted-foreground">-</span>
                                        )}
                                    </TableCell>
                                    <TableCell>
                                        {ticket.confidenceScore !== undefined && ticket.confidenceScore !== null ? (
                                            <div className="flex items-center gap-2">
                                                <div className="w-16 h-2 bg-secondary rounded-full overflow-hidden">
                                                    <div
                                                        className={`h-full ${ticket.confidenceScore > 0.8 ? 'bg-green-500' : ticket.confidenceScore > 0.5 ? 'bg-yellow-500' : 'bg-red-500'}`}
                                                        style={{ width: `${ticket.confidenceScore * 100}%` }}
                                                    />
                                                </div>
                                                <span className="text-xs text-muted-foreground">{(ticket.confidenceScore * 100).toFixed(0)}%</span>
                                            </div>
                                        ) : (
                                            <span className="text-xs text-muted-foreground">-</span>
                                        )}
                                    </TableCell>
                                </TableRow>
                            ))
                        )}
                    </TableBody>
                </Table>
            </div>
        </div>
    );
}
