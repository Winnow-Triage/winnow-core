import { useQuery, useQueryClient } from "@tanstack/react-query";
import { api, searchClusters, type ClusterSearchDto } from "@/lib/api";
import { useProject } from "@/context/ProjectContext";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Link } from "react-router-dom";
import { Input } from "@/components/ui/input";
import { useState, useEffect } from "react";
import { Merge, RefreshCw, AlertCircle, ShieldAlert } from "lucide-react";
import { PageTitle } from "@/components/ui/page-title";



export default function Clusters() {
  const [search, setSearch] = useState("");
  const [sortBy, setSortBy] = useState<"size" | "criticality" | "newest">(
    "criticality",
  );
  const [selectedIds, setSelectedIds] = useState<string[]>([]);
  const [isMerging, setIsMerging] = useState(false);
  const queryClient = useQueryClient();
  const { currentProject } = useProject();

  // Debounce the search input
  const [debouncedSearch, setDebouncedSearch] = useState(search);

  // Sync the debounced value
  useEffect(() => {
    const handler = setTimeout(() => {
      setDebouncedSearch(search);
    }, 500);
    return () => clearTimeout(handler);
  }, [search]);

  const {
    data: clusters,
    isLoading,
    error,
    refetch,
  } = useQuery<ClusterSearchDto[]>({
    queryKey: ["clusters", currentProject?.id, debouncedSearch],
    queryFn: async () => {
      if (!debouncedSearch) {
        const { data } = await api.get(`/clusters?sort=${sortBy}`);
        // Map backend Cluster to ClusterSearchDto to conform to type
        return data; 
      }
      const data = await searchClusters(debouncedSearch, 1, 50);
      return data.items;
    },
    staleTime: 30 * 1000,
    enabled: !!currentProject,
    retry: 0,
  });

  const sortedClusters = [...(clusters || [])].sort((a, b) => {
    if (debouncedSearch) {
      // If actively searching, maintain the backend relevance order (which is default index order)
      return 0;
    }
    if (sortBy === "size") {
      return (b.reportCount || 0) - (a.reportCount || 0);
    }
    if (sortBy === "criticality") {
      return (b.criticalityScore || 0) - (a.criticalityScore || 0);
    }
    if (sortBy === "newest") {
      return new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime();
    }
    return 0;
  });

  const handleMerge = async () => {
    if (selectedIds.length < 2) return;

    setIsMerging(true);
    try {
      const [targetId, ...sourceIds] = selectedIds;
      await api.post(`/clusters/${targetId}/merge`, { sourceIds });
      await queryClient.invalidateQueries({ queryKey: ["clusters"] });
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
    setSelectedIds((prev) =>
      prev.includes(id) ? prev.filter((i) => i !== id) : [...prev, id],
    );
  };

  if (error) {
    return (
      <div className="flex flex-col items-center justify-center h-[60vh] text-center p-8">
        <AlertCircle className="h-12 w-12 text-destructive mb-4" />
        <h3 className="text-xl font-bold">Access Denied</h3>
        <p className="text-muted-foreground mt-2 max-w-md">
          {(error as any).response?.data?.detail || 
           (error as any).response?.data?.message || 
           "You don't have permission to view clusters in this project."}
        </p>
      </div>
    );
  }

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-col md:flex-row md:items-end justify-between gap-6">
        <div className="flex flex-col gap-1">
          <PageTitle>Active Clusters</PageTitle>
          <p className="text-muted-foreground">
            Aggregated alerts for rapid triaging and response.
          </p>
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
              {isMerging ? (
                <RefreshCw className="h-4 w-4 animate-spin" />
              ) : (
                <Merge className="h-4 w-4" />
              )}
              Merge {selectedIds.length} Clusters
            </Button>
          )}
          <div className="flex items-center gap-2">
            <span className="text-sm text-muted-foreground whitespace-nowrap">
              Sort by:
            </span>
            <select
              className="bg-background border rounded px-2 py-1 h-10 text-sm outline-none focus:ring-1 focus:ring-ring"
              value={sortBy}
              onChange={(e) => setSortBy(e.target.value as any)}
            >
              <option value="size">Cluster Size</option>
              <option value="criticality">Criticality</option>
              <option value="newest">Newest</option>
            </select>
          </div>
          <div className="min-w-[200px]">
            <Input
              placeholder="Search clusters..."
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
              <TableHead>Cluster Title</TableHead>
              <TableHead>Status</TableHead>
              <TableHead>Criticality</TableHead>
              <TableHead>Created</TableHead>
              <TableHead className="text-right">Related Reports</TableHead>
              {debouncedSearch && <TableHead>Relevance</TableHead>}
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading ? (
              <TableRow>
                <TableCell colSpan={debouncedSearch ? 7 : 6} className="h-24 text-center">
                  Loading...
                </TableCell>
              </TableRow>
            ) : sortedClusters.length === 0 ? (
              <TableRow>
                <TableCell colSpan={debouncedSearch ? 7 : 6} className="h-24 text-center">
                  No clusters found.
                </TableCell>
              </TableRow>
            ) : (
              sortedClusters.map((cluster) => {
                const isSelected = selectedIds.includes(cluster.id);
                return (
                  <TableRow
                    key={cluster.id}
                    className="cursor-pointer"
                    data-state={isSelected ? "selected" : undefined}
                    onClick={() => toggleSelection(cluster.id)}
                  >
                    <TableCell>
                      <input
                        type="checkbox"
                        className="h-4 w-4 rounded border-gray-300 text-primary focus:ring-primary"
                        checked={isSelected}
                        onChange={() => toggleSelection(cluster.id)}
                        onClick={(e) => e.stopPropagation()}
                      />
                    </TableCell>
                    <TableCell className="font-medium">
                      <div className="flex items-center gap-2">
                        {cluster.isLocked && (
                          <ShieldAlert className="h-4 w-4 text-red-500 shrink-0" />
                        )}
                        {!cluster.isLocked && cluster.isOverage && (
                          <AlertCircle className="h-4 w-4 text-amber-500 shrink-0" />
                        )}
                        <Link
                          to={`/clusters/${cluster.id}`}
                          onClick={(e) => e.stopPropagation()}
                          className={`hover:underline block font-semibold ${cluster.isLocked ? "text-red-600 dark:text-red-400" : ""}`}
                        >
                          {cluster.isLocked
                            ? "Locked Cluster (Limit Exceeded)"
                            : cluster.title || "Untitled Cluster"}
                        </Link>
                      </div>
                    </TableCell>
                    <TableCell>
                      <Badge
                        variant={
                          cluster.status === "Closed" ? "success" : "neutral"
                        }
                        className="rounded-full"
                      >
                        {cluster.status}
                      </Badge>
                    </TableCell>
                    <TableCell>
                      {cluster.criticalityScore !== null ? (
                        <Badge
                          variant={
                            cluster.criticalityScore >= 8
                              ? "critical"
                              : cluster.criticalityScore >= 5
                                ? "warning"
                                : "success"
                          }
                        >
                          {cluster.criticalityScore}
                        </Badge>
                      ) : (
                        <span className="text-muted-foreground opacity-50 px-4">
                          —
                        </span>
                      )}
                    </TableCell>
                    <TableCell>
                      {new Date(cluster.createdAt).toLocaleDateString()}
                    </TableCell>
                    <TableCell className="text-right">
                      <Badge
                        variant={
                          cluster.reportCount > 1 ? "default" : "secondary"
                        }
                      >
                        {cluster.reportCount}
                      </Badge>
                    </TableCell>
                    {debouncedSearch && (
                      <TableCell>
                        {cluster.relevanceScore !== undefined &&
                        cluster.relevanceScore !== null ? (
                          <div className="flex items-center gap-2">
                            <div className="w-16 h-2 bg-secondary rounded-full overflow-hidden">
                              <div
                                className={`h-full ${cluster.relevanceScore > 0.05 ? "bg-green-500" : cluster.relevanceScore > 0.02 ? "bg-yellow-500" : "bg-blue-500"}`}
                                style={{
                                  width: `${Math.min(cluster.relevanceScore * 1000, 100)}%`,
                                }}
                              />
                            </div>
                            <span className="text-xs text-muted-foreground mr-1">
                              {(cluster.relevanceScore * 1000).toFixed(0)}
                            </span>
                          </div>
                        ) : (
                          <span className="text-xs text-muted-foreground">-</span>
                        )}
                      </TableCell>
                    )}
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
