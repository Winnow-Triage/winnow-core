import { useQuery, useQueryClient, keepPreviousData } from "@tanstack/react-query";
import { api, searchReports } from "@/lib/api";
import { useProject } from "@/context/ProjectContext";
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
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Link } from "react-router-dom";
import { Input } from "@/components/ui/input";
import { useState, useEffect } from "react";
import { AlertCircle, ShieldAlert, Merge, RefreshCw, ChevronLeft, ChevronRight } from "lucide-react";
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

export default function AllReports() {
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [sortConfig, setSortConfig] = useState<{
    key: keyof Report;
    direction: "asc" | "desc";
  } | null>(null);
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

  // Query: Either all reports, or search results if there is a query
  const { data, isLoading, error } = useQuery({
    queryKey: ["reports", currentProject?.id, debouncedSearch, page, pageSize],
    queryFn: async () => {
      return await searchReports(debouncedSearch || "", page, pageSize);
    },
    enabled: !!currentProject,
    retry: 0,
    placeholderData: keepPreviousData,
  });

  const reports = data?.items || [];
  const totalCount = data?.totalCount || 0;
  const totalPages = Math.ceil(totalCount / pageSize);

  const sortedReports = [...(reports || [])].sort((a, b) => {
    if (!sortConfig) return 0;
    const { key, direction } = sortConfig;

    const aValue = (a as any)[key] ?? (key === "relevanceScore" ? 0 : "");
    const bValue = (b as any)[key] ?? (key === "relevanceScore" ? 0 : "");

    if (aValue < bValue) return direction === "asc" ? -1 : 1;
    if (aValue > bValue) return direction === "asc" ? 1 : -1;
    return 0;
  });

  const handleSort = (key: keyof Report) => {
    let direction: "asc" | "desc" = "asc";
    if (
      sortConfig &&
      sortConfig.key === key &&
      sortConfig.direction === "asc"
    ) {
      direction = "desc";
    }
    setSortConfig({ key, direction });
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

  if (error) {
    return (
      <div className="flex flex-col items-center justify-center h-[60vh] text-center p-8">
        <AlertCircle className="h-12 w-12 text-destructive mb-4" />
        <h3 className="text-xl font-bold">Access Denied</h3>
        <p className="text-muted-foreground mt-2 max-w-md">
          {(error as any).response?.data?.detail || 
           (error as any).response?.data?.message || 
           "You don't have permission to view reports in this project."}
        </p>
      </div>
    );
  }

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-col md:flex-row md:items-end justify-between gap-6">
        <div className="flex flex-col gap-1">
          <PageTitle>All Reports</PageTitle>
          <p className="text-muted-foreground">
            Comprehensive view of all ingested issues and telemetry.
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
              <TableHead
                className="cursor-pointer"
                onClick={() => handleSort("title")}
              >
                Title
              </TableHead>
              <TableHead
                className="cursor-pointer"
                onClick={() => handleSort("status")}
              >
                Status
              </TableHead>
              <TableHead
                className="cursor-pointer"
                onClick={() => handleSort("updatedAt")}
              >
                Updated
              </TableHead>
              <TableHead>Cluster</TableHead>
              <TableHead
                className="cursor-pointer"
                onClick={() => handleSort("relevanceScore")}
              >
                Relevance
              </TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading ? (
              <TableRow>
                <TableCell colSpan={5} className="h-24 text-center">
                  Loading...
                </TableCell>
              </TableRow>
            ) : sortedReports.length === 0 ? (
              <TableRow>
                <TableCell colSpan={5} className="h-24 text-center">
                  No reports found.
                </TableCell>
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
                        onClick={(e) => e.stopPropagation()}
                      />
                    </TableCell>
                    <TableCell className="font-medium">
                      <div className="flex items-center gap-2">
                        {report.isLocked && (
                          <ShieldAlert className="h-4 w-4 text-red-500 shrink-0" />
                        )}
                        {!report.isLocked && report.isOverage && (
                          <AlertCircle className="h-4 w-4 text-amber-500 shrink-0" />
                        )}
                        <Link
                          to={`/reports/${report.id}`}
                          onClick={(e) => e.stopPropagation()}
                          className={`hover:underline block ${report.isLocked ? "text-red-600 dark:text-red-400" : ""}`}
                        >
                          {report.isLocked
                            ? "Locked Report (Limit Exceeded)"
                            : report.title || report.description}
                        </Link>
                      </div>
                    </TableCell>
                    <TableCell>
                      <Badge
                        variant={
                          report.status === "Closed"
                            ? "success"
                            : report.status === "Duplicate"
                              ? "muted"
                              : "neutral"
                        }
                      >
                        {report.status}
                      </Badge>
                    </TableCell>
                    <TableCell>
                      {new Date(report.updatedAt).toLocaleDateString()}
                    </TableCell>
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
                      {report.relevanceScore !== undefined &&
                        report.relevanceScore !== null ? (
                        <div className="flex items-center gap-2">
                          <div className="w-16 h-2 bg-secondary rounded-full overflow-hidden">
                            <div
                              className={`h-full ${report.relevanceScore > 0.05 ? "bg-green-500" : report.relevanceScore > 0.02 ? "bg-yellow-500" : "bg-blue-500"}`}
                              style={{
                                width: `${Math.min(report.relevanceScore * 1000, 100)}%`,
                              }}
                            />
                          </div>
                          <span className="text-xs text-muted-foreground mr-1">
                            {(report.relevanceScore * 1000).toFixed(0)}
                          </span>
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
      <div className="flex flex-col sm:flex-row items-center justify-between gap-4 px-2">
        <div className="flex items-center gap-4">
          <div className="text-sm text-muted-foreground whitespace-nowrap">
            Showing {reports.length} of {totalCount} reports
          </div>
          <div className="flex items-center gap-2">
            <span className="text-sm text-muted-foreground whitespace-nowrap">Page size:</span>
            <Select
              value={pageSize.toString()}
              onValueChange={(val) => {
                setPageSize(parseInt(val));
                setPage(1);
              }}
            >
              <SelectTrigger className="h-8 w-[70px]">
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
        <div className="flex items-center space-x-2">
          <Button
            variant="outline"
            size="sm"
            onClick={() => setPage((p) => Math.max(1, p - 1))}
            disabled={page === 1 || isLoading}
          >
            <ChevronLeft className="h-4 w-4 mr-1" />
            Previous
          </Button>
          <div className="text-sm font-medium">
            Page {page} of {Math.max(1, totalPages)}
          </div>
          <Button
            variant="outline"
            size="sm"
            onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
            disabled={page >= totalPages || isLoading}
          >
            Next
            <ChevronRight className="h-4 w-4 ml-1" />
          </Button>
        </div>
      </div>
    </div>
  );
}
