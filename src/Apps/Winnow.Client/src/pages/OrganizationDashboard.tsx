import { useQuery } from "@tanstack/react-query";
import {
  getOrganizationMetrics,
  type OrganizationDashboardMetrics,
} from "@/lib/api";
import {
  AlertCircle,
  Loader2,
  Building2,
  LayoutDashboard,
  Layers,
} from "lucide-react";
import { BarChart, Bar, XAxis, Tooltip, Legend } from "recharts";
import { useChartDimensions } from "@/hooks/use-chart-dimensions";

import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Progress } from "@/components/ui/progress";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { Badge } from "@/components/ui/badge";
import { PageTitle } from "@/components/ui/page-title";

export default function OrganizationDashboard() {
  const { data, isLoading, error } = useQuery<OrganizationDashboardMetrics>({
    queryKey: ["orgDashboardMetrics"],
    queryFn: getOrganizationMetrics,
    refetchInterval: 30000,
  });

  const { ref, dimensions } = useChartDimensions();

  if (isLoading) {
    return (
      <div className="flex h-full items-center justify-center">
        <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
      </div>
    );
  }

  if (error) {
    return (
      <div className="p-8">
        <div className="bg-destructive/15 text-destructive p-4 rounded-md border border-destructive/20 flex gap-2 items-center">
          <AlertCircle className="h-4 w-4" />
          <div>
            <p className="font-semibold">Error</p>
            <p className="text-sm">
              Failed to load organization metrics. {(error as Error).message}
            </p>
          </div>
        </div>
      </div>
    );
  }

  if (!data) return null;

  const hasLimit = typeof data.quota.baseLimit === "number";
  const quotaPercentage = hasLimit
    ? Math.min((data.quota.totalUsage / data.quota.baseLimit!) * 100, 100)
    : 0;
  const isNearingLimit =
    hasLimit && quotaPercentage >= 80 && !data.quota.isOverage;

  return (
    <div className="space-y-6 pt-4">
      <div className="flex flex-col gap-1">
        <PageTitle>Organization Overview</PageTitle>
        <p className="text-muted-foreground">
          High-level capacity, cross-team performance, and resource usage.
        </p>
      </div>

      <div className="grid gap-6 md:grid-cols-3">
        {/* Quota Usage */}
        <Card className="col-span-1 md:col-span-3 lg:col-span-1">
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium flex items-center gap-2">
              <Layers className="h-4 w-4 text-muted-foreground" />
              Monthly Report Usage
            </CardTitle>
            <div className="flex items-baseline gap-1 mt-1">
              <span className="text-3xl font-bold">
                {data.quota.totalUsage.toLocaleString()}
              </span>
              <span className="text-sm text-muted-foreground">
                {hasLimit
                  ? `/ ${data.quota.baseLimit!.toLocaleString()}`
                  : " reports"}
              </span>
            </div>
          </CardHeader>
          <CardContent>
            {hasLimit ? (
              <div className="space-y-2 mt-4">
                <Progress
                  value={quotaPercentage}
                  className={`h-2 ${data.quota.isOverage ? "text-destructive" : isNearingLimit ? "text-amber-500" : ""}`}
                />
                <div className="flex justify-between text-xs text-muted-foreground">
                  <span>{quotaPercentage.toFixed(1)}% used</span>
                  {data.quota.isOverage ? (
                    <span className="text-destructive font-medium">
                      Over limit
                    </span>
                  ) : isNearingLimit ? (
                    <span className="text-amber-500 font-medium">
                      Nearing limit
                    </span>
                  ) : (
                    <span>Healthy</span>
                  )}
                </div>
              </div>
            ) : (
              <div className="space-y-4 mt-4">
                <div ref={ref} className="h-[120px] w-full relative">
                  {dimensions.width > 0 && dimensions.height > 0 ? (
                    <BarChart
                      id="org-usage-bar-chart"
                      width={dimensions.width}
                      height={dimensions.height}
                      data={data.quota.usageHistory}
                      margin={{ top: 0, right: 0, left: 0, bottom: 0 }}
                    >
                      <XAxis
                        dataKey="month"
                        axisLine={false}
                        tickLine={false}
                        tick={{
                          fontSize: 10,
                          fill: "hsl(var(--muted-foreground))",
                        }}
                        dy={5}
                        hide={dimensions.width < 250}
                      />
                      <Tooltip
                        cursor={{ fill: "hsl(var(--muted)/0.5)" }}
                        contentStyle={{
                          backgroundColor: "hsl(var(--background))",
                          borderColor: "hsl(var(--border))",
                          borderRadius: "6px",
                          fontSize: "12px",
                        }}
                        itemStyle={{ color: "hsl(var(--foreground))" }}
                        formatter={(value: any, name?: string) => [
                          value?.toLocaleString() || "0",
                          name || "",
                        ]}
                        labelStyle={{
                          color: "hsl(var(--muted-foreground))",
                          marginBottom: "4px",
                        }}
                      />
                      <Legend />
                      <Bar
                        dataKey="reportCount"
                        name="Reports"
                        fill="hsl(var(--primary))"
                        radius={[4, 4, 0, 0]}
                      />
                      <Bar
                        dataKey="clusterCount"
                        name="Clusters"
                        fill="hsl(var(--muted-foreground))"
                        radius={[4, 4, 0, 0]}
                      />
                    </BarChart>
                  ) : (
                    <div className="h-full w-full bg-gray-50/50 dark:bg-white/5 animate-pulse rounded-md" />
                  )}
                </div>
                <div className="flex justify-between text-xs text-muted-foreground border-t pt-2 border-dashed">
                  <span>6-Month History</span>
                  <span className="text-primary font-medium">
                    Unlimited Quota
                  </span>
                </div>
              </div>
            )}
          </CardContent>
        </Card>
      </div>

      <div className="grid gap-6 grid-cols-1 lg:grid-cols-2">
        {/* Team Breakdown */}
        <Card className="col-span-1">
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Building2 className="h-5 w-5" />
              Team Activity
            </CardTitle>
            <CardDescription>
              Report volume generated by teams this month.
            </CardDescription>
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
              {data.teamBreakdown.length > 0 ? (
                data.teamBreakdown.map((team) => (
                  <div
                    key={team.teamId}
                    className="flex items-center justify-between"
                  >
                    <div className="space-y-1">
                      <p className="text-sm font-medium leading-none">
                        {team.teamName}
                      </p>
                      <p className="text-xs text-muted-foreground">
                        {team.projectCount}{" "}
                        {team.projectCount === 1 ? "project" : "projects"}
                      </p>
                    </div>
                    <div className="flex items-center gap-4">
                      <Badge variant="secondary" className="font-mono">
                        {team.reportVolume.toLocaleString()} reports
                      </Badge>
                    </div>
                  </div>
                ))
              ) : (
                <div className="text-sm text-muted-foreground py-4 text-center border rounded-md border-dashed">
                  No teams found in this organization.
                </div>
              )}
            </div>
          </CardContent>
        </Card>

        {/* Top Projects */}
        <Card className="col-span-1">
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <LayoutDashboard className="h-5 w-5" />
              Top Projects
            </CardTitle>
            <CardDescription>
              Projects with the highest report volume (30 days).
            </CardDescription>
          </CardHeader>
          <CardContent>
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Project</TableHead>
                  <TableHead className="text-right">Volume</TableHead>
                  <TableHead className="text-right">Active Clusters</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {data.topProjects.length > 0 ? (
                  data.topProjects.map((project) => (
                    <TableRow key={project.projectId}>
                      <TableCell className="font-medium">
                        {project.projectName}
                      </TableCell>
                      <TableCell className="text-right">
                        {project.reportCount.toLocaleString()}
                      </TableCell>
                      <TableCell className="text-right">
                        <Badge
                          variant={
                            project.activeClusters > 10
                              ? "destructive"
                              : "secondary"
                          }
                        >
                          {project.activeClusters}
                        </Badge>
                      </TableCell>
                    </TableRow>
                  ))
                ) : (
                  <TableRow>
                    <TableCell
                      colSpan={3}
                      className="text-center text-muted-foreground py-4"
                    >
                      No active projects found.
                    </TableCell>
                  </TableRow>
                )}
              </TableBody>
            </Table>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
