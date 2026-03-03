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

interface Cluster {
    id: string;
    title: string | null;
    summary: string | null;
    criticalityScore: number | null;
    status: string;
    createdAt: string;
    reportCount: number;
    isLocked: boolean;
    isOverage: boolean;
}

export default function Clusters() {
    const [search, setSearch] = useState('');
    const [sortBy, setSortBy] = useState<'size' | 'criticality' | 'newest'>('criticality');
    const [selectedIds, setSelectedIds] = useState<string[]>([]);
    const [isMerging, setIsMerging] = useState(false);
    const queryClient = useQueryClient();
    const { currentProject } = useProject();

    const { data: clusters, isLoading, refetch } = useQuery<Cluster[]>({
        queryKey: ['clusters', currentProject?.id, sortBy],
        queryFn: async () => {
            const { data } = await api.get(`/clusters?sort=${sortBy}`);
            return data;
        },
        staleTime: 30 * 1000,
        enabled: !!currentProject,
    });

    const filteredClusters = clusters?.filter(c =>
        (c.title || c.summary || '').toLowerCase().includes(search.toLowerCase())
    ) || [];

    const handleMerge = async () => {
        if (selectedIds.length < 2) return;

        setIsMerging(true);
        try {
            const [targetId, ...sourceIds] = selectedIds;
            await api.post(`/clusters/${targetId}/merge`, { sourceIds });
            await queryClient.invalidateQueries({ queryKey: ['clusters'] });
            await refetch();
            setSelectedIds([]);
        } catch (e) {
            console.error("Failed to merge clusters", e);
            alert("Merge failed. Check the console for details.");
        } finally {
            setIsMerging(false);
        }
    };

    const toggleSelection = (id: string) => {
        setSelectedIds(prev =>
            prev.includes(id) ? prev.filter(i => i !== id) : [...prev, id]
        );
    };

    // Helper to get criticality color
    const getCriticalityColor = (score: number | null) => {
        if (score === null) return 'text-muted-foreground';
        if (score >= 8) return 'text-red-500 font-bold';
        if (score >= 5) return 'text-amber-500 font-bold';
        return 'text-blue-500';
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
                        ) : filteredClusters.length === 0 ? (
                            <TableRow>
                                <TableCell colSpan={6} className="h-24 text-center">No clusters found.</TableCell>
                            </TableRow>
                        ) : (
                            filteredClusters.map((cluster) => {
                                const isSelected = selectedIds.includes(cluster.id);
                                return (
                                    <TableRow key={cluster.id} className={isSelected ? 'bg-muted/50' : ''}>
                                        <TableCell>
                                            <input
                                                type="checkbox"
                                                className="h-4 w-4 rounded border-gray-300 text-primary focus:ring-primary"
                                                checked={isSelected}
                                                onChange={() => toggleSelection(cluster.id)}
                                            />
                                        </TableCell>
                                        <TableCell className="font-medium">
                                            <div className="flex items-center gap-2">
                                                {cluster.isLocked && <ShieldAlert className="h-4 w-4 text-red-500 shrink-0" />}
                                                {!cluster.isLocked && cluster.isOverage && <AlertCircle className="h-4 w-4 text-amber-500 shrink-0" />}
                                                <Link to={`/clusters/${cluster.id}`} className={`hover:underline block font-semibold ${cluster.isLocked ? 'text-red-600 dark:text-red-400' : ''}`}>
                                                    {cluster.isLocked ? 'Locked Cluster (Limit Exceeded)' : (cluster.title || "Untitled Cluster")}
                                                </Link>
                                            </div>
                                        </TableCell>
                                        <TableCell>
                                            <Badge variant="outline">{cluster.status}</Badge>
                                        </TableCell>
                                        <TableCell>
                                            <span className={`text-sm ${getCriticalityColor(cluster.criticalityScore)}`}>
                                                {cluster.criticalityScore ?? '—'}
                                            </span>
                                        </TableCell>
                                        <TableCell>{new Date(cluster.createdAt).toLocaleDateString()}</TableCell>
                                        <TableCell className="text-right">
                                            <Badge variant={cluster.reportCount > 1 ? "default" : "secondary"}>
                                                {cluster.reportCount}
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
