import { useQuery, useQueryClient, keepPreviousData } from "@tanstack/react-query";
import { api, searchClusters, type PaginatedSearchList, type ClusterSearchDto } from "@/lib/api";
import { useProject } from "@/hooks/use-project";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  DropdownMenu,
  DropdownMenuCheckboxItem,
  DropdownMenuContent,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Link } from "react-router-dom";
import { Input } from "@/components/ui/input";
import { useState, useEffect } from "react";
import { 
  Merge, 
  RefreshCw, 
  ShieldAlert,
  ChevronLeft,
  ChevronRight,
  ListFilter,
  ArrowUpDown,
  ArrowUp,
  ArrowDown,
  X
} from "lucide-react";
import { PageHeader, LoadingState, ErrorState } from "@/components/layout/PageState";

const CLUSTER_STATUSES = ["Open", "Exported", "Dismissed", "Merged"];

export default function Clusters() {
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);

  // Filtering state
  const [selectedStatuses, setSelectedStatuses] = useState<string[]>([]);
  const [isOverage, setIsOverage] = useState<boolean | undefined>(undefined);
  const [isLocked, setIsLocked] = useState<boolean | undefined>(undefined);

  const [sortConfig, setSortConfig] = useState<{
    key: "title" | "status" | "createdAt" | "criticalityScore" | "reportCount" | "relevanceScore";
    direction: "asc" | "desc";
  }>({ key: "criticalityScore", direction: "desc" });

  const [lastNonSearchSort, setLastNonSearchSort] = useState(sortConfig);

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
      setPage(1);
    }, 500);
    return () => clearTimeout(handler);
  }, [search]);

  // Auto-sort by relevance when searching
  useEffect(() => {
    if (debouncedSearch) {
      setSortConfig({ key: "relevanceScore", direction: "desc" });
    } else {
      setSortConfig(lastNonSearchSort);
    }
  }, [debouncedSearch, lastNonSearchSort]);

  const {
    data,
    isLoading,
    isPlaceholderData,
    error,
    refetch,
  } = useQuery<PaginatedSearchList<ClusterSearchDto>>({
    queryKey: [
      "clusters", 
      currentProject?.id, 
      debouncedSearch, 
      page, 
      pageSize, 
      selectedStatuses,
      isOverage,
      isLocked,
      sortConfig
    ],
    queryFn: async () => {
      const sortByMap: Record<string, string> = {
        title: "title",
        status: "status",
        createdAt: "createdAt",
        criticalityScore: "criticalityScore",
        reportCount: "reportCount",
        relevanceScore: "relevanceScore"
      };

      return await searchClusters(
        debouncedSearch || "",
        page,
        pageSize,
        selectedStatuses.length > 0 ? selectedStatuses : undefined,
        isOverage,
        isLocked,
        sortByMap[sortConfig.key] || "createdAt",
        sortConfig.direction === "asc" ? "Asc" : "Desc"
      );
    },
    staleTime: 30 * 1000,
    enabled: !!currentProject,
    retry: 0,
    placeholderData: keepPreviousData,
    refetchInterval: (query) => 
      query.state.data?.items?.some(c => c.isSummarizing) ? 3000 : false,
  });

  const clusters = data?.items || [];
  const totalCount = data?.totalCount || 0;
  const totalPages = Math.ceil(totalCount / pageSize);

  const handleSort = (key: typeof sortConfig.key) => {
    const newConfig: { key: typeof sortConfig.key; direction: "asc" | "desc" } = {
      key,
      direction: sortConfig.key === key && sortConfig.direction === "desc" ? "asc" : "desc",
    };
    
    setSortConfig(newConfig);
    if (!search) {
      setLastNonSearchSort(newConfig);
    }
    setPage(1);
  };

  const toggleStatus = (status: string) => {
    setSelectedStatuses((prev) =>
      prev.includes(status) ? prev.filter((s) => s !== status) : [...prev, status]
    );
    setPage(1);
  };

  const clearFilters = () => {
    setSelectedStatuses([]);
    setIsOverage(undefined);
    setIsLocked(undefined);
    setPage(1);
  };

  const handleMerge = async () => {
    if (selectedIds.length < 2) return;
    setIsMerging(true);
    try {
      const [targetId, ...sourceIds] = selectedIds;
      await api.post(`/clusters/${targetId}/merge`, { sourceIds });
      await queryClient.invalidateQueries({ queryKey: ["clusters"] });
      setSelectedIds([]);
    } catch (e) {
      console.error("Failed to merge clusters", e);
    } finally {
      setIsMerging(false);
    }
  };

  const toggleSelection = (id: string) => {
    setSelectedIds((prev) =>
      prev.includes(id) ? prev.filter((i) => i !== id) : [...prev, id],
    );
  };

  const SortIcon = ({ column }: { column: typeof sortConfig.key }) => {
    if (sortConfig.key !== column) return <ArrowUpDown className="ml-2 h-4 w-4 text-muted-foreground" />;
    return sortConfig.direction === "asc" ? (
      <ArrowUp className="ml-2 h-4 w-4 text-primary" />
    ) : (
      <ArrowDown className="ml-2 h-4 w-4 text-primary" />
    );
  };

  if (isLoading && !isPlaceholderData) return <LoadingState message="Fetching your clusters..." />;

  if (error) {
    return (
      <ErrorState 
        title="Error Displaying Clusters"
        message={
          (error as { response?: { data?: { detail?: string; message?: string } } }).response?.data?.detail ||
          (error as { response?: { data?: { message?: string } } }).response?.data?.message ||
          "Failed to load clusters for this project."
        }
        onRetry={() => refetch()}
      />
    );
  }

  const hasActiveFilters = selectedStatuses.length > 0 || isOverage !== undefined || isLocked !== undefined;

  return (
    <div className="flex flex-col gap-6">
      <PageHeader 
        title="Active Clusters" 
        description="Aggregated alerts for rapid triaging and response."
      >
        <div className="flex flex-col items-end gap-3">
          <div className="flex items-center gap-3 w-full justify-end">
            {selectedIds.length >= 2 && (
              <Button
                variant="default"
                size="sm"
                className="flex items-center gap-2 bg-blue-600 hover:bg-blue-700 h-10 px-4 whitespace-nowrap"
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
            <div className="flex-1 md:min-w-[300px] relative">
              <Input
                placeholder="Search clusters..."
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                className="h-10 pr-10"
              />
              {isLoading && (
                <div className="absolute right-3 top-3">
                  <RefreshCw className="h-4 w-4 animate-spin text-muted-foreground" />
                </div>
              )}
            </div>
            
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button variant="outline" className="h-10 flex items-center gap-2">
                  <ListFilter className="h-4 w-4" />
                  Filters
                  {hasActiveFilters && (
                    <Badge variant="secondary" className="ml-1 px-1 h-5 min-w-[20px] justify-center">
                      {(selectedStatuses.length + (isOverage !== undefined ? 1 : 0) + (isLocked !== undefined ? 1 : 0))}
                    </Badge>
                  )}
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end" className="w-[200px]">
                <DropdownMenuLabel>Status</DropdownMenuLabel>
                <DropdownMenuSeparator />
                {CLUSTER_STATUSES.map((status) => (
                  <DropdownMenuCheckboxItem
                    key={status}
                    checked={selectedStatuses.includes(status)}
                    onCheckedChange={() => toggleStatus(status)}
                  >
                    {status}
                  </DropdownMenuCheckboxItem>
                ))}
                
                <DropdownMenuSeparator />
                <DropdownMenuLabel>Flags</DropdownMenuLabel>
                <DropdownMenuSeparator />
                <DropdownMenuCheckboxItem
                  checked={isOverage === true}
                  onCheckedChange={(checked) => setIsOverage(checked ? true : undefined)}
                >
                  Overage only
                </DropdownMenuCheckboxItem>
                <DropdownMenuCheckboxItem
                  checked={isLocked === true}
                  onCheckedChange={(checked) => setIsLocked(checked ? true : undefined)}
                >
                  Locked only
                </DropdownMenuCheckboxItem>
                
                {hasActiveFilters && (
                  <>
                    <DropdownMenuSeparator />
                    <Button 
                      variant="ghost" 
                      className="w-full justify-start text-xs h-8 text-destructive hover:text-destructive"
                      onClick={clearFilters}
                    >
                      <X className="h-3 w-3 mr-2" />
                      Clear all filters
                    </Button>
                  </>
                )}
              </DropdownMenuContent>
            </DropdownMenu>
          </div>
          
          {hasActiveFilters && (
            <div className="flex flex-wrap items-center gap-2 animate-in fade-in slide-in-from-top-1 justify-end">
              {selectedStatuses.map(s => (
                <Badge key={s} variant="secondary" className="flex items-center gap-1 py-1 pr-1">
                  {s}
                  <X 
                    className="h-3 w-3 cursor-pointer hover:text-destructive" 
                    onClick={() => toggleStatus(s)} 
                  />
                </Badge>
              ))}
              {isOverage && (
                <Badge variant="secondary" className="flex items-center gap-1 py-1 pr-1">
                  Overage
                  <X className="h-3 w-3 cursor-pointer hover:text-destructive" onClick={() => setIsOverage(undefined)} />
                </Badge>
              )}
              {isLocked && (
                <Badge variant="secondary" className="flex items-center gap-1 py-1 pr-1">
                  Locked
                  <X className="h-3 w-3 cursor-pointer hover:text-destructive" onClick={() => setIsLocked(undefined)} />
                </Badge>
              )}
            </div>
          )}
        </div>
      </PageHeader>

      <div className="border rounded-md overflow-hidden bg-card shadow-sm">
        <Table>
          <TableHeader>
            <TableRow className="hover:bg-transparent border-b">
              <TableHead className="w-[40px]"></TableHead>
              <TableHead
                className="cursor-pointer hover:bg-muted/50 transition-colors py-3"
                onClick={() => handleSort("title")}
              >
                <div className="flex items-center">
                  Cluster Title <SortIcon column="title" />
                </div>
              </TableHead>
              <TableHead
                className="cursor-pointer hover:bg-muted/50 transition-colors py-3"
                onClick={() => handleSort("status")}
              >
                <div className="flex items-center">
                  Status <SortIcon column="status" />
                </div>
              </TableHead>
              <TableHead
                className="cursor-pointer hover:bg-muted/50 transition-colors py-3"
                onClick={() => handleSort("criticalityScore")}
              >
                <div className="flex items-center">
                  Criticality <SortIcon column="criticalityScore" />
                </div>
              </TableHead>
              <TableHead
                className="cursor-pointer hover:bg-muted/50 transition-colors py-3"
                onClick={() => handleSort("createdAt")}
              >
                <div className="flex items-center">
                  Created <SortIcon column="createdAt" />
                </div>
              </TableHead>
              <TableHead
                className="cursor-pointer hover:bg-muted/50 transition-colors py-3 text-right"
                onClick={() => handleSort("reportCount")}
              >
                <div className="flex items-center justify-end">
                  Reports <SortIcon column="reportCount" />
                </div>
              </TableHead>
              <TableHead
                className="cursor-pointer hover:bg-muted/50 transition-colors py-3"
                onClick={() => handleSort("relevanceScore")}
              >
                <div className="flex items-center">
                  Relevance <SortIcon column="relevanceScore" />
                </div>
              </TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {clusters.length === 0 ? (
              <TableRow>
                <TableCell colSpan={7} className="p-0">
                  <div className="py-12">
                    <ErrorState 
                      title="No clusters found"
                      message="Try adjusting your search filters to find what you're looking for."
                      onRetry={hasActiveFilters ? clearFilters : undefined}
                    />
                  </div>
                </TableCell>
              </TableRow>
            ) : (
              clusters.map((cluster) => {
                const isSelected = selectedIds.includes(cluster.id);
                return (
                  <TableRow
                    key={cluster.id}
                    className="cursor-pointer transition-colors hover:bg-muted/50"
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
                    <TableCell className="font-medium max-w-[400px]">
                      <div className="flex items-center gap-2">
                        {cluster.isLocked && (
                          <ShieldAlert className="h-4 w-4 text-red-500 shrink-0" />
                        )}
                        {!cluster.isLocked && cluster.isOverage && (
                          <X className="h-4 w-4 text-amber-500 shrink-0" />
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
                        {cluster.isSummarizing && (
                          <Badge variant="outline" className="ml-1 animate-pulse bg-purple-500/10 text-purple-500 border-purple-500/20 text-[10px] h-5 py-0">
                            Summarizing...
                          </Badge>
                        )}
                      </div>
                    </TableCell>
                    <TableCell>
                      <Badge
                        variant={
                          cluster.status === "Exported" || cluster.status === "Dismissed" || cluster.status === "Merged"
                            ? "default"
                            : "secondary"
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
                              ? "destructive"
                              : cluster.criticalityScore >= 5
                                ? "outline"
                                : "secondary"
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
                    <TableCell className="text-muted-foreground text-sm">
                      {new Date(cluster.createdAt).toLocaleDateString(undefined, {
                        month: 'short',
                        day: 'numeric',
                        year: 'numeric'
                      })}
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
                    <TableCell>
                      {cluster.relevanceScore !== undefined &&
                      cluster.relevanceScore !== null ? (
                        <div className="flex items-center gap-3">
                          <div className="w-16 h-1.5 bg-secondary rounded-full overflow-hidden shrink-0">
                            <div
                              className={`h-full transition-all duration-500 ${cluster.relevanceScore > 0.05 ? "bg-green-500" : cluster.relevanceScore > 0.02 ? "bg-yellow-500" : "bg-blue-400"}`}
                              style={{
                                width: `${Math.min(cluster.relevanceScore * 1000, 100)}%`,
                              }}
                            />
                          </div>
                          <span className="text-xs font-medium text-muted-foreground w-6 text-right">
                            {(cluster.relevanceScore * 1000).toFixed(0)}
                          </span>
                        </div>
                      ) : (
                        <span className="text-xs text-muted-foreground opacity-40">-</span>
                      )}
                    </TableCell>
                  </TableRow>
                );
              })
            )}
          </TableBody>
        </Table>
      </div>

      <div className="flex flex-col sm:flex-row items-center justify-between gap-4 px-2 py-2 border-t mt-auto">
        <div className="flex items-center gap-6">
          <div className="text-sm text-muted-foreground whitespace-nowrap font-medium">
            Total results: <span className="text-foreground">{totalCount.toLocaleString()}</span>
          </div>
          <div className="flex items-center gap-3">
            <span className="text-sm text-muted-foreground whitespace-nowrap">Page size:</span>
            <Select
              value={pageSize.toString()}
              onValueChange={(val) => {
                setPageSize(parseInt(val));
                setPage(1);
              }}
            >
              <SelectTrigger className="h-8 w-[70px] bg-background">
                <SelectValue placeholder={pageSize.toString()} />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="20">20</SelectItem>
                <SelectItem value="50">50</SelectItem>
                <SelectItem value="100">100</SelectItem>
              </SelectContent>
            </Select>
          </div>
        </div>
        <div className="flex items-center space-x-3">
          <Button
            variant="outline"
            size="sm"
            className="h-9 px-4"
            onClick={() => setPage((p) => Math.max(1, p - 1))}
            disabled={page === 1 || isLoading}
          >
            <ChevronLeft className="h-4 w-4 mr-2" />
            Previous
          </Button>
          <div className="text-sm font-semibold min-w-[100px] text-center">
            {page} <span className="text-muted-foreground font-normal mx-1">of</span> {Math.max(1, totalPages)}
          </div>
          <Button
            variant="outline"
            size="sm"
            className="h-9 px-4"
            onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
            disabled={page >= totalPages || isLoading}
          >
            Next
            <ChevronRight className="h-4 w-4 ml-2" />
          </Button>
        </div>
      </div>
    </div>
  );
}
