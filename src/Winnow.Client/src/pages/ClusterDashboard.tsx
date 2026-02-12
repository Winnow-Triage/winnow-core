import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { api } from '@/lib/api';
import { Link } from 'react-router-dom';
import { ArrowUpDown } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import {
    Table,
    TableBody,
    TableCell,
    TableHead,
    TableHeader,
    TableRow,
} from '@/components/ui/table';
import { Badge } from '@/components/ui/badge';
import { Area, AreaChart, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts';
import {
    DropdownMenu,
    DropdownMenuContent,
    DropdownMenuItem,
    DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";

interface Ticket {
    id: string;
    title: string;
    description: string;
    status: string;
    createdAt: string;
    parentTicketId?: string;
    criticalityScore?: number;
}

// Mock data for the chart
const chartData = [
    { time: '10:00', value: 12 },
    { time: '11:00', value: 18 },
    { time: '12:00', value: 15 },
    { time: '13:00', value: 25 },
    { time: '14:00', value: 32 },
    { time: '15:00', value: 28 },
];

export default function ClusterDashboard() {
    const [sortBy, setSortBy] = useState<'newest' | 'criticality' | 'confidence'>('newest');

    const { data: tickets, isLoading } = useQuery<Ticket[]>({
        queryKey: ['tickets', sortBy],
        queryFn: async () => {
            const { data } = await api.get(`/tickets?sort=${sortBy}`);
            return data;
        },
        staleTime: 60 * 1000,
    });



    if (isLoading) return <div>Loading tickets...</div>;

    const clusters = tickets?.filter(t => !t.parentTicketId && t.status !== 'Duplicate') || [];

    return (
        <div className="flex flex-col gap-6">
            <div className="flex items-center justify-between">
                <h1 className="text-3xl font-bold tracking-tight">Cluster Dashboard</h1>
            </div>

            {/* ... (Summary Cards remain the same) ... */}

            <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
                <Card>
                    <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                        <CardTitle className="text-sm font-medium">Active Clusters</CardTitle>
                    </CardHeader>
                    <CardContent>
                        <div className="text-2xl font-bold">{clusters.length}</div>
                        <p className="text-xs text-muted-foreground">+2 from last hour</p>
                    </CardContent>
                </Card>
                <Card>
                    <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                        <CardTitle className="text-sm font-medium">Total Tickets</CardTitle>
                    </CardHeader>
                    <CardContent>
                        <div className="text-2xl font-bold">{tickets?.length || 0}</div>
                        <p className="text-xs text-muted-foreground">+12% from yesterday</p>
                    </CardContent>
                </Card>
                <Card>
                    <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                        <CardTitle className="text-sm font-medium">Escalation Rate</CardTitle>
                    </CardHeader>
                    <CardContent>
                        <div className="text-2xl font-bold">4.3%</div>
                        <p className="text-xs text-muted-foreground">-0.1% from last week</p>
                    </CardContent>
                </Card>
                <Card>
                    <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                        <CardTitle className="text-sm font-medium">Avg Resolution Time</CardTitle>
                    </CardHeader>
                    <CardContent>
                        <div className="text-2xl font-bold">24m</div>
                        <p className="text-xs text-muted-foreground">+2m from yesterday</p>
                    </CardContent>
                </Card>
            </div>

            {/* ... (Chart remains the same) ... */}

            <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-7">
                <Card className="col-span-4">
                    <CardHeader>
                        <CardTitle>Ticket Volume</CardTitle>
                        <CardDescription>Incoming tickets over the last 6 hours.</CardDescription>
                    </CardHeader>
                    <CardContent className="pl-2">
                        <ResponsiveContainer width="100%" height={300}>
                            <AreaChart data={chartData}>
                                <defs>
                                    <linearGradient id="colorValue" x1="0" y1="0" x2="0" y2="1">
                                        <stop offset="5%" stopColor="hsl(var(--primary))" stopOpacity={0.3} />
                                        <stop offset="95%" stopColor="hsl(var(--primary))" stopOpacity={0} />
                                    </linearGradient>
                                </defs>
                                <XAxis dataKey="time" stroke="#888888" fontSize={12} tickLine={false} axisLine={false} />
                                <YAxis stroke="#888888" fontSize={12} tickLine={false} axisLine={false} tickFormatter={(value) => `${value}`} />
                                <Tooltip />
                                <Area type="monotone" dataKey="value" stroke="hsl(var(--primary))" fillOpacity={1} fill="url(#colorValue)" />
                            </AreaChart>
                        </ResponsiveContainer>
                    </CardContent>
                </Card>

                <Card className="col-span-3">
                    <CardHeader className="flex flex-row items-center justify-between">
                        <div>
                            <CardTitle>Recent Activity</CardTitle>
                            <CardDescription>Latest tickets processed.</CardDescription>
                        </div>
                        <DropdownMenu>
                            <DropdownMenuTrigger asChild>
                                <Button variant="outline" size="sm" className="ml-auto">
                                    <ArrowUpDown className="mr-2 h-4 w-4" />
                                    Sort by: {sortBy.charAt(0).toUpperCase() + sortBy.slice(1)}
                                </Button>
                            </DropdownMenuTrigger>
                            <DropdownMenuContent align="end">
                                <DropdownMenuItem onClick={() => setSortBy('newest')}>Newest</DropdownMenuItem>
                                <DropdownMenuItem onClick={() => setSortBy('criticality')}>Criticality</DropdownMenuItem>
                                <DropdownMenuItem onClick={() => setSortBy('confidence')}>Confidence</DropdownMenuItem>
                            </DropdownMenuContent>
                        </DropdownMenu>
                    </CardHeader>
                    <CardContent>
                        <Table>
                            <TableHeader>
                                <TableRow>
                                    <TableHead>Title</TableHead>
                                    <TableHead>Status</TableHead>
                                    <TableHead>Criticality</TableHead>
                                </TableRow>
                            </TableHeader>
                            <TableBody>
                                {tickets?.slice(0, 10).map((ticket) => (
                                    <TableRow key={ticket.id} className="cursor-pointer hover:bg-muted/50">
                                        <TableCell className="font-medium truncate max-w-[200px]">
                                            <Link to={`/tickets/${ticket.id}`} className="block w-full h-full hover:underline">
                                                {ticket.title}
                                            </Link>
                                            {ticket.parentTicketId && (
                                                <div className="text-xs text-muted-foreground mt-1 flex items-center gap-1">
                                                    <span>↳ Duplicate of</span>
                                                    <Link to={`/tickets/${ticket.parentTicketId}`} className="text-primary hover:underline font-mono">
                                                        {ticket.parentTicketId.substring(0, 8)}...
                                                    </Link>
                                                </div>
                                            )}
                                        </TableCell>
                                        <TableCell>
                                            <Badge variant={ticket.status === 'Exported' ? 'default' : ticket.status === 'Duplicate' ? 'secondary' : 'outline'}>
                                                {ticket.status}
                                            </Badge>
                                        </TableCell>
                                        <TableCell>
                                            {ticket.criticalityScore && (
                                                <Badge variant="outline" className={`
                                                    ${ticket.criticalityScore >= 8 ? 'bg-red-100 text-red-800 border-red-200 dark:bg-red-900/30' :
                                                        ticket.criticalityScore >= 5 ? 'bg-amber-100 text-amber-800 border-amber-200 dark:bg-amber-900/30' :
                                                            'bg-blue-100 text-blue-800 border-blue-200 dark:bg-blue-900/30'}
                                                `}>
                                                    {ticket.criticalityScore}/10
                                                </Badge>
                                            )}
                                        </TableCell>
                                    </TableRow>
                                ))}
                            </TableBody>
                        </Table>
                    </CardContent>
                </Card>
            </div>
        </div>
    );
}
