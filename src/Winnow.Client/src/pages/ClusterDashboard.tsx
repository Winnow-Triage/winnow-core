import { useQuery } from '@tanstack/react-query';
import { api } from '@/lib/api';
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
import { Link } from 'react-router-dom';

interface Ticket {
    id: string;
    title: string;
    description: string;
    status: string;
    createdAt: string;
    parentTicketId?: string;
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
    const { data: tickets, isLoading } = useQuery<Ticket[]>({
        queryKey: ['tickets'],
        queryFn: async () => {
            const { data } = await api.get('/tickets');
            return data;
        },
    });

    if (isLoading) return <div>Loading tickets...</div>;

    const clusters = tickets?.filter(t => !t.parentTicketId && t.status !== 'Duplicate') || [];

    return (
        <div className="flex flex-col gap-6">
            <div className="flex items-center justify-between">
                <h1 className="text-3xl font-bold tracking-tight">Cluster Dashboard</h1>
            </div>

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
                    <CardHeader>
                        <CardTitle>Recent Activity</CardTitle>
                        <CardDescription>Latest tickets processed.</CardDescription>
                    </CardHeader>
                    <CardContent>
                        <Table>
                            <TableHeader>
                                <TableRow>
                                    <TableHead>Title</TableHead>
                                    <TableHead>Status</TableHead>
                                </TableRow>
                            </TableHeader>
                            <TableBody>
                                {tickets?.slice(0, 5).map((ticket) => (
                                    <TableRow key={ticket.id} className="cursor-pointer hover:bg-muted/50">
                                        <TableCell className="font-medium truncate max-w-[150px]">
                                            <Link to={`/tickets/${ticket.id}`} className="block w-full h-full">
                                                {ticket.title}
                                            </Link>
                                        </TableCell>
                                        <TableCell>
                                            <Badge variant={ticket.status === 'Exported' ? 'default' : 'secondary'}>
                                                {ticket.status}
                                            </Badge>
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
