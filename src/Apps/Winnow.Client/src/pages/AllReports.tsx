import { useQuery, useQueryClient } from '@tanstack/react-query';
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
import { Button } from '@/components/ui/button';
import { Link } from 'react-router-dom';
import { Input } from '@/components/ui/input';
import { useState } from 'react';
import { AlertCircle, ShieldAlert, Merge, RefreshCw } from 'lucide-react';
import { PageTitle } from '@/components/ui/page-title';


interface Report {
    id: string;
    title: string;
    message: string;
    status: string;
    createdAt: string;
    clusterId?: string;
    confidenceScore?: number;
    isOverage?: boolean;
    isLocked?: boolean;
}

export default function AllReports() {
    const [search, setSearch] = useState('');
    const [sortConfig, setSortConfig] = useState<{ key: keyof Report; direction: 'asc' | 'desc' } | null>(null);
    const [selectedIds, setSelectedIds] = useState<string[]>([]);
    const [isMerging, setIsMerging] = useState(false);
    const { currentProject } = useProject();
    const queryClient = useQueryClient();

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

    const toggleSelection = (id: string) => {
        setSelectedIds(prev =>
            prev.includes(id) ? prev.filter(i => i !== id) : [...prev, id]
        );
    };

    const handleMerge = async () => {
        if (selectedIds.length < 2) return;

        setIsMerging(true);
        try {
            const [targetId, ...sourceIds] = selectedIds;
            await api.post(`/reports/${targetId}/merge`, { sourceIds });
            await queryClient.invalidateQueries({ queryKey: ['reports'] });
            setSelectedIds([]);
        } catch (e) {
            console.error("Failed to group reports", e);
            alert("Grouping failed. Check the console for details.");
        } finally {
            setIsMerging(false);
        }
    };

    return (
        <div className="flex flex-col gap-6">
            <div className="flex flex-col md:flex-row md:items-end justify-between gap-6">
                <div className="flex flex-col gap-1">
                    <PageTitle>All Reports</PageTitle>
                    <p className="text-muted-foreground">Comprehensive view of all ingested issues and telemetry.</p>
                </div>
                <div className="flex items-center gap-4 justify-end">
                    {selectedIds.length >= 2 && (
                        <Button
                            variant="default"
                            size="sm"
                            className="flex items-center gap-2 bg-blue-600 hover:bg-blue-700 h-10 px-4"
                            onClick={handleMerge}
                            disabled={isMerging}
                        >
                            {isMerging ? <RefreshCw className="h-4 w-4 animate-spin" /> : <Merge className="h-4 w-4" />}
                            Group {selectedIds.length} Reports
                        </Button>
                    )}
                    <div className="min-w-[250px]">
                        <Input
                            placeholder="Search reports..."
                            value={search}
                            onChange={(e) => setSearch(e.target.value)}
                            className="h-10"
                        />
                    </div>
                </div>
            </div>

            <div className="border rounded-md">
                <Table>
                    <TableHeader>
                        <TableRow>
                            <TableHead className="w-[40px]"></TableHead>
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
                            sortedReports.map((report) => {
                                const isSelected = selectedIds.includes(report.id);
                                return (
                                    <TableRow
                                        key={report.id}
                                        className="cursor-pointer"
                                        data-state={isSelected ? "selected" : undefined}
                                        onClick={() => toggleSelection(report.id)}
                                    >
                                        <TableCell>
                                            <input
                                                type="checkbox"
                                                className="h-4 w-4 rounded border-gray-300 text-primary focus:ring-primary"
                                                checked={isSelected}
                                                onChange={() => toggleSelection(report.id)}
                                            />
                                        </TableCell>
                                        <TableCell className="font-medium">
                                            <div className="flex items-center gap-2">
                                                {report.isLocked && <ShieldAlert className="h-4 w-4 text-red-500 shrink-0" />}
                                                {!report.isLocked && report.isOverage && <AlertCircle className="h-4 w-4 text-amber-500 shrink-0" />}
                                                <Link to={`/reports/${report.id}`} className={`hover:underline block ${report.isLocked ? 'text-red-600 dark:text-red-400' : ''}`}>
                                                    {report.isLocked ? 'Locked Report (Limit Exceeded)' : (report.title || report.message)}
                                                </Link>
                                            </div>
                                        </TableCell>
                                        <TableCell>
                                            <Badge variant={report.status === 'Closed' ? 'success' : report.status === 'Duplicate' ? 'muted' : 'neutral'}>
                                                {report.status}
                                            </Badge>
                                        </TableCell>
                                        <TableCell>{new Date(report.createdAt).toLocaleDateString()}</TableCell>
                                        <TableCell>
                                            {report.clusterId ? (
                                                <Badge variant="outline" className="text-xs">
                                                    {report.clusterId.substring(0, 8)}
                                                </Badge>
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
                                );
                            })
                        )}
                    </TableBody>
                </Table>
            </div>
        </div>
    );
}
