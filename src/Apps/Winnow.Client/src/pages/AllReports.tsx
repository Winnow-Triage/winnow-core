import { useQuery, useQueryClient, keepPreviousData } from "@tanstack/react-query";
import { api, searchReports, type PaginatedSearchList } from "@/lib/api";
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
  AlertCircle, 
  ShieldAlert, 
  Merge, 
  RefreshCw, 
  ChevronLeft, 
  ChevronRight, 
  Filter, 
  ListFilter,
  ArrowUpDown,
  ArrowUp,
  ArrowDown,
  X
} from "lucide-react";
import { PageTitle } from "@/components/ui/page-title";

interface Report {
  id: string;
  title: string;
  description: string;
  status: string;
  updatedAt: string;
  clusterId?: string;
  relevanceScore?: number;
  isOverage?: boolean;
  isLocked?: boolean;
}

const REPORT_STATUSES = ["Open", "Duplicate", "Resolved", "Dismissed", "Exported"];

export default function AllReports() {
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  
  // Filtering state
  const [selectedStatuses, setSelectedStatuses] = useState<string[]>([]);
  const [isOverage, setIsOverage] = useState<boolean | undefined>(undefined);
  const [isLocked, setIsLocked] = useState<boolean | undefined>(undefined);
  
  const [sortConfig, setSortConfig] = useState<{
    key: "title" | "status" | "updatedAt" | "relevanceScore";
    direction: "asc" | "desc";
  }>({ key: "updatedAt", direction: "desc" });

  const [lastNonSearchSort, setLastNonSearchSort] = useState(sortConfig);

  const [selectedIds, setSelectedIds] = useState<string[]>([]);
  const [isMerging, setIsMerging] = useState(false);
  const { currentProject } = useProject();
  const queryClient = useQueryClient();

  // Debounce the search input
  const [debouncedSearch, setDebouncedSearch] = useState(search);

  // Sync the debounced value
  useEffect(() => {
    const handler = setTimeout(() => {
      setDebouncedSearch(search);
      setPage(1); // Reset to first page on new search
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

  // Query: Now includes all filters and server-side sorting
  const { data, isLoading, isPlaceholderData, error } = useQuery<PaginatedSearchList<Report>>({
    queryKey: [
      "reports", 
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
      // Map frontend sort keys to backend fields
      const sortByMap: Record<string, string> = {
        title: "Title",
        status: "Status",
        updatedAt: "CreatedAt",
        relevanceScore: "Relevance"
      };

      return await searchReports(
        debouncedSearch || "", 
        page, 
        pageSize,
        selectedStatuses.length > 0 ? selectedStatuses : undefined,
        undefined, // clusterId
        isOverage,
        isLocked,
        undefined, // assignedTo
        sortByMap[sortConfig.key] || "CreatedAt",
        sortConfig.direction === "asc" ? "Asc" : "Desc"
      );
    },
    enabled: !!currentProject,
    retry: 0,
    placeholderData: keepPreviousData,
  });

  const reports: Report[] = data?.items || [];
  const totalCount = data?.totalCount || 0;
  const totalPages = Math.ceil(totalCount / pageSize);

  const handleSort = (key: typeof sortConfig.key) => {
    const newConfig: { key: typeof sortConfig.key; direction: "asc" | "desc" } = {
      key,
      direction: sortConfig.key === key && sortConfig.direction === "desc" ? "asc" : "desc",
    };
    
    setSortConfig(newConfig);
    
    // Only capture as manual preference if not currently searching
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

  const toggleSelection = (id: string) => {
    setSelectedIds((prev) =>
      prev.includes(id) ? prev.filter((i) => i !== id) : [...prev, id],
    );
  };

  const handleMerge = async () => {
    if (selectedIds.length < 2) return;

    setIsMerging(true);
    try {
      const [targetId, ...sourceIds] = selectedIds;
      await api.post(`/reports/${targetId}/merge`, { sourceIds });
      await queryClient.invalidateQueries({ queryKey: ["reports"] });
      setSelectedIds([]);
    } catch (e) {
      console.error("Failed to group reports", e);
      alert("Grouping failed. Check the console for details.");
    } finally {
      setIsMerging(false);
    }
  };

  const SortIcon = ({ column }: { column: typeof sortConfig.key }) => {
    if (sortConfig.key !== column) return <ArrowUpDown className="ml-2 h-4 w-4 text-muted-foreground" />;
    return sortConfig.direction === "asc" ? (
      <ArrowUp className="ml-2 h-4 w-4 text-primary" />
    ) : (
      <ArrowDown className="ml-2 h-4 w-4 text-primary" />
    );
  };

  if (error) {
    return (
      <div className="flex flex-col items-center justify-center h-[60vh] text-center p-8">
        <AlertCircle className="h-12 w-12 text-destructive mb-4" />
        <h3 className="text-xl font-bold">Access Denied</h3>
        <p className="text-muted-foreground mt-2 max-w-md">
          {(error as { response?: { data?: { detail?: string; message?: string } } }).response?.data?.detail ||
            (error as { response?: { data?: { message?: string } } }).response?.data?.message ||
            "You don't have permission to view reports in this project."}
        </p>
      </div>
    );
  }

  const hasActiveFilters = selectedStatuses.length > 0 || isOverage !== undefined || isLocked !== undefined;

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-col md:flex-row md:items-end justify-between gap-6">
        <div className="flex flex-col gap-1">
          <PageTitle>All Reports</PageTitle>
          <p className="text-muted-foreground">
            Comprehensive view of all ingested issues and telemetry.
          </p>
        </div>
        <div className="flex flex-col items-end gap-3">
          <div className="flex items-center gap-3 w-full">
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
                Group {selectedIds.length} Reports
              </Button>
            )}
            <div className="flex-1 md:min-w-[300px] relative">
              <Input
                placeholder="Search reports..."
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
                {REPORT_STATUSES.map((status) => (
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
            <div className="flex flex-wrap items-center gap-2 animate-in fade-in slide-in-from-top-1">
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
      </div>

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
                  Title <SortIcon column="title" />
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
                onClick={() => handleSort("updatedAt")}
              >
                <div className="flex items-center">
                  Updated <SortIcon column="updatedAt" />
                </div>
              </TableHead>
              <TableHead className="py-3">Cluster</TableHead>
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
            {isLoading && !isPlaceholderData ? (
              <TableRow>
                <TableCell colSpan={6} className="h-64 text-center">
                  <div className="flex flex-col items-center justify-center gap-2">
                    <RefreshCw className="h-8 w-8 animate-spin text-muted-foreground" />
                    <p className="text-muted-foreground font-medium">Loading reports...</p>
                  </div>
                </TableCell>
              </TableRow>
            ) : reports.length === 0 ? (
              <TableRow>
                <TableCell colSpan={6} className="h-64 text-center">
                  <div className="flex flex-col items-center justify-center gap-2">
                    <Filter className="h-8 w-8 text-muted-foreground opacity-50" />
                    <h3 className="font-semibold text-lg">No reports found</h3>
                    <p className="text-muted-foreground text-sm max-w-xs mx-auto">
                      Try adjusting your search filters to find what you're looking for.
                    </p>
                    {hasActiveFilters && (
                      <Button variant="outline" size="sm" onClick={clearFilters} className="mt-2">
                        Reset all filters
                      </Button>
                    )}
                  </div>
                </TableCell>
              </TableRow>
            ) : (
              reports.map((report) => {
                const isSelected = selectedIds.includes(report.id);
                return (
                  <TableRow
                    key={report.id}
                    className="cursor-pointer transition-colors hover:bg-muted/50"
                    data-state={isSelected ? "selected" : undefined}
                    onClick={() => toggleSelection(report.id)}
                  >
                    <TableCell>
                      <input
                        type="checkbox"
                        className="h-4 w-4 rounded border-gray-300 text-primary focus:ring-primary"
                        checked={isSelected}
                        onChange={() => toggleSelection(report.id)}
                        onClick={(e) => e.stopPropagation()}
                      />
                    </TableCell>
                    <TableCell className="font-medium max-w-[400px]">
                      <div className="flex items-start gap-2">
                        <div className="mt-1 shrink-0">
                          {report.isLocked ? (
                            <ShieldAlert className="h-4 w-4 text-red-500" />
                          ) : report.isOverage ? (
                            <AlertCircle className="h-4 w-4 text-amber-500" />
                          ) : null}
                        </div>
                        <div className="flex flex-col gap-0.5 min-w-0">
                          <Link
                            to={`/reports/${report.id}`}
                            onClick={(e) => e.stopPropagation()}
                            className={`hover:underline block truncate font-semibold ${report.isLocked ? "text-red-600 dark:text-red-400" : ""}`}
                          >
                            {report.isLocked
                              ? "Locked Report (Limit Exceeded)"
                              : report.title || "Untitled Report"}
                          </Link>
                          <span className="text-xs text-muted-foreground truncate opacity-70">
                            {report.description || "No description provided"}
                          </span>
                        </div>
                      </div>
                    </TableCell>
                    <TableCell>
                      <Badge
                        variant={
                          report.status === "Resolved"
                            ? "success"
                            : report.status === "Duplicate"
                              ? "muted"
                              : report.status === "Dismissed"
                                ? "destructive"
                                : "neutral"
                        }
                        className="font-medium px-2 py-0"
                      >
                        {report.status}
                      </Badge>
                    </TableCell>
                    <TableCell className="text-muted-foreground text-sm">
                      {new Date(report.updatedAt).toLocaleDateString(undefined, {
                        month: 'short',
                        day: 'numeric',
                        year: 'numeric'
                      })}
                    </TableCell>
                    <TableCell>
                      {report.clusterId ? (
                        <Link 
                          to={`/clusters/${report.clusterId}`}
                          onClick={(e) => e.stopPropagation()}
                          className="hover:underline"
                        >
                          <Badge variant="outline" className="text-[10px] font-mono tracking-tighter opacity-80 h-5 px-1 bg-muted/30 hover:bg-muted">
                            {report.clusterId.substring(0, 8)}
                          </Badge>
                        </Link>
                      ) : (
                        <span className="text-xs text-muted-foreground opacity-40 italic">Unassigned</span>
                      )}
                    </TableCell>
                    <TableCell>
                      {report.relevanceScore !== undefined &&
                        report.relevanceScore !== null ? (
                        <div className="flex items-center gap-3">
                          <div className="w-16 h-1.5 bg-secondary rounded-full overflow-hidden shrink-0">
                            <div
                              className={`h-full transition-all duration-500 ${report.relevanceScore > 0.05 ? "bg-green-500" : report.relevanceScore > 0.02 ? "bg-yellow-500" : "bg-blue-400"}`}
                              style={{
                                width: `${Math.min(report.relevanceScore * 1000, 100)}%`,
                              }}
                            />
                          </div>
                          <span className="text-xs font-medium text-muted-foreground w-6 text-right">
                            {(report.relevanceScore * 1000).toFixed(0)}
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
