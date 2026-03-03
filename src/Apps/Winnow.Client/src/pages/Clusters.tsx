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
    clusterId?: string;
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

    // Group reports by clusterId to count members
    const clusterMap = new Map<string, { count: number; reports: Report[] }>();
    reports?.forEach(t => {
        if (t.clusterId) {
            const entry = clusterMap.get(t.clusterId) || { count: 0, reports: [] };
            entry.count += 1;
            entry.reports.push(t);
            clusterMap.set(t.clusterId, entry);
        }
    });

    // Build a unique cluster list from the first report in each cluster
    const clusterEntries = Array.from(clusterMap.entries())
        .map(([clusterId, entry]) => ({
            clusterId,
            representative: entry.reports[0],
            count: entry.count,
        }))
        .filter(c => (c.representative.title || c.representative.message || '').toLowerCase().includes(search.toLowerCase()));

    // Sort based on selected metric
    const sortedClusters = [...clusterEntries].sort((a, b) => {
        if (sortBy === 'size') {
            return b.count - a.count;
        }
        if (sortBy === 'criticality') {
            // Criticality now lives on the cluster entity (not shown in list DTO)
            return b.count - a.count;
        }
        return new Date(b.representative.createdAt).getTime() - new Date(a.representative.createdAt).getTime();
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
                            sortedClusters.map((cluster) => {
                                const report = cluster.representative;
                                const isSelected = selectedIds.includes(cluster.clusterId);
                                return (
                                    <TableRow key={cluster.clusterId} className={isSelected ? 'bg-muted/50' : ''}>
                                        <TableCell>
                                            <input
                                                type="checkbox"
                                                className="h-4 w-4 rounded border-gray-300 text-primary focus:ring-primary"
                                                checked={isSelected}
                                                onChange={() => toggleSelection(cluster.clusterId)}
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
                                            <span className="text-xs text-muted-foreground italic">—</span>
                                        </TableCell>
                                        <TableCell>{new Date(report.createdAt).toLocaleDateString()}</TableCell>
                                        <TableCell className="text-right">
                                            <Badge variant={cluster.count > 1 ? "default" : "secondary"}>
                                                {cluster.count}
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
