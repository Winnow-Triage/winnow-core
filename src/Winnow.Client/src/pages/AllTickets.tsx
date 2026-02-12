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
}

export default function AllTickets() {
    const [search, setSearch] = useState('');

    const { data: tickets, isLoading } = useQuery<Ticket[]>({
        queryKey: ['tickets'],
        queryFn: async () => {
            // TODO: Implement server-side filtering/pagination later
            const { data } = await api.get('/tickets');
            return data;
        },
    });

    const filteredTickets = tickets?.filter(t =>
        t.title.toLowerCase().includes(search.toLowerCase()) ||
        t.status.toLowerCase().includes(search.toLowerCase())
    ) || [];

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
                            <TableHead>Title</TableHead>
                            <TableHead>Status</TableHead>
                            <TableHead>Created</TableHead>
                            <TableHead>Cluster</TableHead>
                        </TableRow>
                    </TableHeader>
                    <TableBody>
                        {isLoading ? (
                            <TableRow>
                                <TableCell colSpan={4} className="h-24 text-center">Loading...</TableCell>
                            </TableRow>
                        ) : filteredTickets.length === 0 ? (
                            <TableRow>
                                <TableCell colSpan={4} className="h-24 text-center">No tickets found.</TableCell>
                            </TableRow>
                        ) : (
                            filteredTickets.map((ticket) => (
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
                                </TableRow>
                            ))
                        )}
                    </TableBody>
                </Table>
            </div>
        </div>
    );
}
