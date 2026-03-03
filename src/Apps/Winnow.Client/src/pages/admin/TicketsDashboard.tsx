import { useEffect, useState, useCallback } from "react";
import {
  getAllAdminReports,
  toggleAdminReportLock,
  resetAdminReportOverage,
  getAllOrganizations,
  getOrganizationDetails,
  type AdminReportSummary,
  type PagedAdminReportResponse,
  type OrganizationSummary,
  type ProjectQuotaSummary,
} from "@/lib/api";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Badge } from "@/components/ui/badge";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { toast } from "sonner";
import { formatTimeAgo } from "@/lib/utils";
import { Ticket, Search, Lock, Unlock, Loader2, RefreshCw } from "lucide-react";

export default function TicketsDashboard() {
  const [data, setData] = useState<PagedAdminReportResponse | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState("");
  const [statusFilter, setStatusFilter] = useState("all");
  const [lockFilter, setLockFilter] = useState("all");
  const [organizationIdFilter, setOrganizationIdFilter] = useState("all");
  const [projectIdFilter, setProjectIdFilter] = useState("all");
  const [page, setPage] = useState(1);
  const [isTogglingLock, setIsTogglingLock] = useState<string | null>(null);
  const [isResettingOverage, setIsResettingOverage] = useState<string | null>(
    null,
  );

  const [organizations, setOrganizations] = useState<OrganizationSummary[]>([]);
  const [projects, setProjects] = useState<ProjectQuotaSummary[]>([]);

  useEffect(() => {
    getAllOrganizations().then(setOrganizations).catch(console.error);
  }, []);

  useEffect(() => {
    if (organizationIdFilter === "all") {
      setProjects([]);
      setProjectIdFilter("all");
      return;
    }

    getOrganizationDetails(organizationIdFilter)
      .then((data) => {
        setProjects(data.projectQuotas || []);
        setProjectIdFilter("all"); // reset project filter when org changes
      })
      .catch(console.error);
  }, [organizationIdFilter]);

  const fetchReports = useCallback(async () => {
    setIsLoading(true);
    try {
      const response = await getAllAdminReports({
        page,
        pageSize: 50,
        searchTerm: searchTerm || undefined,
        status: statusFilter !== "all" ? statusFilter : undefined,
        isLocked: lockFilter === "all" ? undefined : lockFilter === "locked",
        organizationId:
          organizationIdFilter !== "all" ? organizationIdFilter : undefined,
        projectId: projectIdFilter !== "all" ? projectIdFilter : undefined,
      });
      setData(response);
    } catch (error) {
      console.error(error);
      toast.error("Failed to fetch reports");
    } finally {
      setIsLoading(false);
    }
  }, [
    page,
    searchTerm,
    statusFilter,
    lockFilter,
    organizationIdFilter,
    projectIdFilter,
  ]);

  useEffect(() => {
    const timeoutId = setTimeout(() => {
      fetchReports();
    }, 300); // Debounce search
    return () => clearTimeout(timeoutId);
  }, [fetchReports]);

  const handleToggleLock = async (reportId: string) => {
    setIsTogglingLock(reportId);
    try {
      const result = await toggleAdminReportLock(reportId);
      toast.success(`Report ${result.isLocked ? "locked" : "unlocked"}`);

      // Optimistically update the local state to avoid a full re-fetch
      if (data) {
        setData({
          ...data,
          items: data.items.map((r) =>
            r.id === reportId ? { ...r, isLocked: result.isLocked } : r,
          ),
        });
      }
    } catch (error) {
      console.error(error);
      toast.error("Failed to toggle lock status");
    } finally {
      setIsTogglingLock(null);
    }
  };

  const handleResetOverage = async (reportId: string) => {
    setIsResettingOverage(reportId);
    try {
      const result = await resetAdminReportOverage(reportId);
      toast.success("Report overage status successfully reset");

      if (data) {
        setData({
          ...data,
          items: data.items.map((r) =>
            r.id === reportId ? { ...r, isOverage: result.isOverage } : r,
          ),
        });
      }
    } catch (error) {
      console.error(error);
      toast.error("Failed to reset overage status");
    } finally {
      setIsResettingOverage(null);
    }
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold tracking-tight text-red-500 flex items-center gap-2">
            <Ticket className="h-8 w-8" />
            System Tickets
          </h1>
          <p className="text-muted-foreground">
            Manage and audit all reports across the entire system.
          </p>
        </div>
        <Button
          variant="outline"
          size="sm"
          onClick={fetchReports}
          disabled={isLoading}
          className="border-red-900/50 text-red-500 hover:bg-red-950/30"
        >
          <RefreshCw
            className={`h-4 w-4 mr-2 ${isLoading ? "animate-spin" : ""}`}
          />
          Refresh
        </Button>
      </div>

      <div className="flex flex-col sm:flex-row gap-4">
        <div className="relative flex-1 w-full space-y-2">
          <label className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
            Search
          </label>
          <div className="relative">
            <Search className="absolute left-2.5 top-2.5 h-4 w-4 text-muted-foreground" />
            <Input
              placeholder="Search by ID or Title..."
              className="pl-8 bg-background/50 border-red-900/50 focus-visible:ring-red-500"
              value={searchTerm}
              onChange={(e) => {
                setSearchTerm(e.target.value);
                setPage(1); // Reset page on search
              }}
            />
          </div>
        </div>
        <div className="w-full sm:w-[180px] space-y-2">
          <label className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
            Status
          </label>
          <Select
            value={statusFilter}
            onValueChange={(v) => {
              setStatusFilter(v);
              setPage(1);
            }}
          >
            <SelectTrigger className="w-full bg-background/50 border-red-900/50">
              <SelectValue placeholder="Status" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All Statuses</SelectItem>
              <SelectItem value="new">New</SelectItem>
              <SelectItem value="open">Open</SelectItem>
              <SelectItem value="investigating">Investigating</SelectItem>
              <SelectItem value="resolved">Resolved</SelectItem>
              <SelectItem value="closed">Closed</SelectItem>
            </SelectContent>
          </Select>
        </div>
        <div className="w-full sm:w-[180px] space-y-2">
          <label className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
            Lock Status
          </label>
          <Select
            value={lockFilter}
            onValueChange={(v) => {
              setLockFilter(v);
              setPage(1);
            }}
          >
            <SelectTrigger className="w-full bg-background/50 border-red-900/50">
              <SelectValue placeholder="Lock Status" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All</SelectItem>
              <SelectItem value="locked">Locked</SelectItem>
              <SelectItem value="unlocked">Unlocked</SelectItem>
            </SelectContent>
          </Select>
        </div>
        <div className="w-full sm:w-[200px] space-y-2">
          <label className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
            Organization
          </label>
          <Select
            value={organizationIdFilter}
            onValueChange={(v) => {
              setOrganizationIdFilter(v);
              setPage(1);
            }}
          >
            <SelectTrigger className="w-full bg-background/50 border-red-900/50">
              <SelectValue placeholder="Organization" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All Organizations</SelectItem>
              {organizations.map((org) => (
                <SelectItem key={org.id} value={org.id}>
                  {org.name}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
        <div className="w-full sm:w-[180px] space-y-2">
          <label className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
            Project
          </label>
          <Select
            value={projectIdFilter}
            onValueChange={(v) => {
              setProjectIdFilter(v);
              setPage(1);
            }}
            disabled={organizationIdFilter === "all"}
          >
            <SelectTrigger className="w-full bg-background/50 border-red-900/50">
              <SelectValue placeholder="Project" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All Projects</SelectItem>
              {projects.map((proj) => (
                <SelectItem key={proj.id} value={proj.id}>
                  {proj.name}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
      </div>

      <div className="rounded-md border border-red-900/50 overflow-hidden bg-background/50 shadow-xl">
        <Table>
          <TableHeader className="bg-red-950/20">
            <TableRow className="border-red-900/50 hover:bg-transparent">
              <TableHead className="text-red-400 font-semibold w-[100px]">
                ID
              </TableHead>
              <TableHead className="text-red-400 font-semibold">
                Title
              </TableHead>
              <TableHead className="text-red-400 font-semibold">
                Tenant
              </TableHead>
              <TableHead className="text-red-400 font-semibold">
                Project
              </TableHead>
              <TableHead className="text-red-400 font-semibold w-[120px]">
                Status
              </TableHead>
              <TableHead className="text-red-400 font-semibold w-[100px]">
                Alarms
              </TableHead>
              <TableHead className="text-red-400 font-semibold w-[150px]">
                Created
              </TableHead>
              <TableHead className="text-red-400 font-semibold text-right">
                Actions
              </TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading && !data ? (
              <TableRow>
                <TableCell
                  colSpan={8}
                  className="h-48 text-center border-red-900/20"
                >
                  <Loader2 className="h-8 w-8 animate-spin mx-auto text-red-500 mb-2" />
                  <p className="text-muted-foreground">Loading tickets...</p>
                </TableCell>
              </TableRow>
            ) : data?.items.length === 0 ? (
              <TableRow>
                <TableCell
                  colSpan={8}
                  className="h-48 text-center text-muted-foreground border-red-900/20"
                >
                  No tickets found matching the current filters.
                </TableCell>
              </TableRow>
            ) : (
              data?.items.map((report: AdminReportSummary) => (
                <TableRow
                  key={report.id}
                  className="border-red-900/20 hover:bg-red-950/10 transition-colors"
                >
                  <TableCell className="font-mono text-xs text-muted-foreground whitespace-nowrap">
                    {report.id.substring(0, 8)}...
                  </TableCell>
                  <TableCell
                    className="font-medium text-foreground max-w-[200px] truncate"
                    title={report.title}
                  >
                    {report.title}
                  </TableCell>
                  <TableCell className="text-sm">
                    <div
                      className="truncate max-w-[150px]"
                      title={report.organizationName}
                    >
                      {report.organizationName}
                    </div>
                  </TableCell>
                  <TableCell className="text-sm text-muted-foreground">
                    <div
                      className="truncate max-w-[150px]"
                      title={report.projectName}
                    >
                      {report.projectName}
                    </div>
                  </TableCell>
                  <TableCell>
                    <Badge
                      variant="outline"
                      className="border-red-900/30 font-normal"
                    >
                      {report.status}
                    </Badge>
                  </TableCell>
                  <TableCell>
                    <div className="flex flex-col gap-1">
                      {report.isLocked ? (
                        <Badge
                          variant="destructive"
                          className="bg-red-900/80 hover:bg-red-900 text-[10px] w-fit py-0 px-1"
                        >
                          Locked
                        </Badge>
                      ) : (
                        <span className="text-[10px] text-muted-foreground flex items-center gap-1">
                          Unlocked
                        </span>
                      )}
                      {report.isOverage && (
                        <Badge
                          variant="outline"
                          className="border-amber-600/50 text-amber-500 bg-amber-600/10 text-[10px] w-fit py-0 px-1"
                        >
                          ⚠️ Overage
                        </Badge>
                      )}
                    </div>
                  </TableCell>
                  <TableCell className="text-xs text-muted-foreground whitespace-nowrap">
                    {formatTimeAgo(report.createdAt)}
                  </TableCell>
                  <TableCell className="text-right">
                    <div className="flex justify-end gap-2">
                      {report.isOverage && (
                        <Button
                          variant="outline"
                          size="sm"
                          onClick={() => handleResetOverage(report.id)}
                          disabled={isResettingOverage === report.id}
                          className="h-8 border-amber-600/50 text-amber-500 hover:bg-amber-600/10 hover:text-amber-400"
                        >
                          {isResettingOverage === report.id ? (
                            <Loader2 className="h-3 w-3 animate-spin" />
                          ) : (
                            <span className="text-[10px] uppercase font-bold tracking-wider">
                              Reset Overage
                            </span>
                          )}
                        </Button>
                      )}
                      <Button
                        variant={report.isLocked ? "outline" : "ghost"}
                        size="sm"
                        onClick={() => handleToggleLock(report.id)}
                        disabled={isTogglingLock === report.id}
                        className={`h-8 ${
                          report.isLocked
                            ? "border-green-500/50 text-green-500 hover:bg-green-500/10 hover:text-green-400"
                            : "text-red-400 hover:text-red-300 hover:bg-red-500/10"
                        }`}
                      >
                        {isTogglingLock === report.id ? (
                          <Loader2 className="h-4 w-4 animate-spin" />
                        ) : report.isLocked ? (
                          <>
                            <Unlock className="h-3 w-3 mr-1" />{" "}
                            <span className="text-[10px] uppercase font-bold tracking-wider">
                              Unlock
                            </span>
                          </>
                        ) : (
                          <>
                            <Lock className="h-3 w-3 mr-1" />{" "}
                            <span className="text-[10px] uppercase font-bold tracking-wider">
                              Lock
                            </span>
                          </>
                        )}
                      </Button>
                    </div>
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </div>

      {/* Pagination */}
      {data && data.totalPages > 1 && (
        <div className="flex items-center justify-between border-t border-red-900/30 pt-4">
          <p className="text-sm text-muted-foreground">
            Showing{" "}
            <span className="font-medium text-foreground">
              {(page - 1) * data.pageSize + 1}
            </span>{" "}
            to{" "}
            <span className="font-medium text-foreground">
              {Math.min(page * data.pageSize, data.totalCount)}
            </span>{" "}
            of{" "}
            <span className="font-medium text-foreground">
              {data.totalCount}
            </span>{" "}
            results
          </p>
          <div className="flex gap-2">
            <Button
              variant="outline"
              size="sm"
              onClick={() => setPage((p) => Math.max(1, p - 1))}
              disabled={page === 1 || isLoading}
              className="border-red-900/50 hover:bg-red-950/30"
            >
              Previous
            </Button>
            <Button
              variant="outline"
              size="sm"
              onClick={() => setPage((p) => Math.min(data.totalPages, p + 1))}
              disabled={page === data.totalPages || isLoading}
              className="border-red-900/50 hover:bg-red-950/30"
            >
              Next
            </Button>
          </div>
        </div>
      )}
    </div>
  );
}
