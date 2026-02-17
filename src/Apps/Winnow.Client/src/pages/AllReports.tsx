import { useQuery } from '@tanstack/react-query';
import { api } from '@/lib/api';
import { useProject } from '@/context/ProjectContext';
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

interface Report {
    id: string;
    title: string;
    message: string;
    status: string;
    createdAt: string;
    parentReportId?: string;
    confidenceScore?: number;
}

export default function AllReports() {
    const [search, setSearch] = useState('');
    const [sortConfig, setSortConfig] = useState<{ key: keyof Report; direction: 'asc' | 'desc' } | null>(null);
    const { currentProject } = useProject();

    const { data: reports, isLoading } = useQuery<Report[]>({
        queryKey: ['reports', currentProject?.id],
        queryFn: async () => {
            const { data } = await api.get('/reports');
            return data;
        },
        enabled: !!currentProject,
    });

    const filteredReports = reports?.filter(t =>
        t.title?.toLowerCase().includes(search.toLowerCase()) ||
        t.message?.toLowerCase().includes(search.toLowerCase()) ||
        t.status.toLowerCase().includes(search.toLowerCase())
    ) || [];

    const sortedReports = [...filteredReports].sort((a, b) => {
        if (!sortConfig) return 0;
        const { key, direction } = sortConfig;

        const aValue = a[key] ?? ((key === 'confidenceScore') ? 0 : '');
        const bValue = b[key] ?? ((key === 'confidenceScore') ? 0 : '');

        if (aValue < bValue) return direction === 'asc' ? -1 : 1;
        if (aValue > bValue) return direction === 'asc' ? 1 : -1;
        return 0;
    });

    const handleSort = (key: keyof Report) => {
        let direction: 'asc' | 'desc' = 'asc';
        if (sortConfig && sortConfig.key === key && sortConfig.direction === 'asc') {
            direction = 'desc';
        }
        setSortConfig({ key, direction });
    };

    return (
        <div className="flex flex-col gap-6">
            <div className="flex items-center justify-between">
                <h1 className="text-3xl font-bold tracking-tight">All Reports</h1>
                <div className="w-1/3">
                    <Input
                        placeholder="Search reports..."
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
                        ) : sortedReports.length === 0 ? (
                            <TableRow>
                                <TableCell colSpan={5} className="h-24 text-center">No reports found.</TableCell>
                            </TableRow>
                        ) : (
                            sortedReports.map((report) => (
                                <TableRow key={report.id}>
                                    <TableCell className="font-medium">
                                        <Link to={`/reports/${report.id}`} className="hover:underline block">
                                            {report.title || report.message}
                                        </Link>
                                    </TableCell>
                                    <TableCell>
                                        <Badge variant="outline">{report.status}</Badge>
                                    </TableCell>
                                    <TableCell>{new Date(report.createdAt).toLocaleDateString()}</TableCell>
                                    <TableCell>
                                        {report.parentReportId ? (
                                            <Link to={`/reports/${report.parentReportId}`} className="text-xs text-muted-foreground hover:underline">
                                                View Parent
                                            </Link>
                                        ) : (
                                            <span className="text-xs text-muted-foreground">-</span>
                                        )}
                                    </TableCell>
                                    <TableCell>
                                        {report.confidenceScore !== undefined && report.confidenceScore !== null ? (
                                            <div className="flex items-center gap-2">
                                                <div className="w-16 h-2 bg-secondary rounded-full overflow-hidden">
                                                    <div
                                                        className={`h-full ${report.confidenceScore > 0.8 ? 'bg-green-500' : report.confidenceScore > 0.5 ? 'bg-yellow-500' : 'bg-red-500'}`}
                                                        style={{ width: `${report.confidenceScore * 100}%` }}
                                                    />
                                                </div>
                                                <span className="text-xs text-muted-foreground">{(report.confidenceScore * 100).toFixed(0)}%</span>
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
