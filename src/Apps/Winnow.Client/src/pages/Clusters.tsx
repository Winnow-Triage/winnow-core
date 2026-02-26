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
import { LayoutDashboard, Merge, RefreshCw, AlertCircle, ShieldAlert } from 'lucide-react';

interface Report {
    id: string;
    title: string;
    message: string;
    status: string;
    createdAt: string;
    parentReportId?: string;
    criticalityScore?: number;
    isOverage?: boolean;
    isLocked?: boolean;
}

export default function Clusters() {
    const [search, setSearch] = useState('');
    const [sortBy, setSortBy] = useState<'size' | 'criticality' | 'newest'>('size');
    const [selectedIds, setSelectedIds] = useState<string[]>([]);
    const [isMerging, setIsMerging] = useState(false);
    const queryClient = useQueryClient();
    const { currentProject } = useProject();

    const { data: reports, isLoading, refetch } = useQuery<Report[]>({
        queryKey: ['reports', currentProject?.id],
        queryFn: async () => {
            const { data } = await api.get('/reports');
            return data;
        },
        staleTime: 60 * 1000,
        enabled: !!currentProject,
    });

    // We need to count children to be useful.
    const clusterMap = new Map<string, number>();
    reports?.forEach(t => {
        if (t.parentReportId) {
            clusterMap.set(t.parentReportId, (clusterMap.get(t.parentReportId) || 0) + 1);
        }
    });

    const clusters = reports?.filter(t => !t.parentReportId && (
        (t.title || t.message || '').toLowerCase().includes(search.toLowerCase())
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

    const handleMerge = async () => {
        if (selectedIds.length < 2) return;

        setIsMerging(true);
        try {
            const [targetId, ...sourceIds] = selectedIds;
            await api.post(`/reports/${targetId}/merge`, { id: targetId, sourceIds });
            await queryClient.invalidateQueries({ queryKey: ['reports'] });
            await refetch();
            setSelectedIds([]);
        } catch (e) {
            console.error("Failed to merge clusters", e);
        } finally {
            setIsMerging(false);
        }
    };

    const toggleSelection = (id: string) => {
        setSelectedIds(prev =>
            prev.includes(id) ? prev.filter(i => i !== id) : [...prev, id]
        );
    };

    return (
        <div className="flex flex-col gap-6">
            <div className="flex items-center justify-between">
                <div className="flex items-center gap-2">
                    <LayoutDashboard className="h-6 w-6 text-muted-foreground" />
                    <h1 className="text-3xl font-bold tracking-tight">Active Clusters</h1>
                </div>
                <div className="flex items-center gap-4 w-2/3 justify-end">
                    {selectedIds.length >= 2 && (
                        <Button
                            variant="default"
                            size="sm"
                            className="flex items-center gap-2 bg-blue-600 hover:bg-blue-700"
                            onClick={handleMerge}
                            disabled={isMerging}
                        >
                            {isMerging ? <RefreshCw className="h-4 w-4 animate-spin" /> : <Merge className="h-4 w-4" />}
                            Merge {selectedIds.length} Clusters
                        </Button>
                    )}
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
                    <div className="w-1/3">
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
                            <TableHead className="w-[40px]"></TableHead>
                            <TableHead>Cluster Title</TableHead>
                            <TableHead>Status</TableHead>
                            <TableHead>Criticality</TableHead>
                            <TableHead>Created</TableHead>
                            <TableHead className="text-right">Related Reports</TableHead>
                        </TableRow>
                    </TableHeader>
                    <TableBody>
                        {isLoading ? (
                            <TableRow>
                                <TableCell colSpan={6} className="h-24 text-center">Loading...</TableCell>
                            </TableRow>
                        ) : sortedClusters.length === 0 ? (
                            <TableRow>
                                <TableCell colSpan={6} className="h-24 text-center">No clusters found.</TableCell>
                            </TableRow>
                        ) : (
                            sortedClusters.map((report) => {
                                const childCount = clusterMap.get(report.id) || 0;
                                const isSelected = selectedIds.includes(report.id);
                                return (
                                    <TableRow key={report.id} className={isSelected ? 'bg-muted/50' : ''}>
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
                                                <Link to={`/reports/${report.id}`} className={`hover:underline block font-semibold ${report.isLocked ? 'text-red-600 dark:text-red-400' : ''}`}>
                                                    {report.isLocked ? 'Locked Cluster (Limit Exceeded)' : (report.title || report.message)}
                                                </Link>
                                            </div>
                                        </TableCell>
                                        <TableCell>
                                            <Badge variant="outline">{report.status}</Badge>
                                        </TableCell>
                                        <TableCell>
                                            {report.criticalityScore ? (
                                                <Badge variant="outline" className={`
                                                    ${report.criticalityScore >= 8 ? 'bg-red-100 text-red-800 border-red-200 dark:bg-red-900/30' :
                                                        report.criticalityScore >= 5 ? 'bg-amber-100 text-amber-800 border-amber-200 dark:bg-amber-900/30' :
                                                            'bg-blue-100 text-blue-800 border-blue-200 dark:bg-blue-900/30'}
                                                `}>
                                                    {report.criticalityScore}/10
                                                </Badge>
                                            ) : (
                                                <span className="text-xs text-muted-foreground italic">Pending...</span>
                                            )}
                                        </TableCell>
                                        <TableCell>{new Date(report.createdAt).toLocaleDateString()}</TableCell>
                                        <TableCell className="text-right">
                                            <Badge variant={childCount > 0 ? "default" : "secondary"}>
                                                {childCount + 1}
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
