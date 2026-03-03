import { useState, useEffect } from "react";
import { useQuery } from "@tanstack/react-query";
import {
  getMyTeams,
  getTeamMetrics,
  type TeamDashboardMetrics,
} from "@/lib/api";
import { AlertCircle, Loader2, Users, LayoutDashboard } from "lucide-react";
import { useTheme } from "@/components/theme-provider";

import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Badge } from "@/components/ui/badge";
import { PageTitle } from "@/components/ui/page-title";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";

import { TriageFunnelChart } from "@/components/dashboard/TriageFunnelChart";
import { HottestClustersList } from "@/components/dashboard/HottestClustersList";

interface Team {
  id: string;
  name: string;
}

export default function TeamDashboard() {
  const { theme } = useTheme();
  const [selectedTeamId, setSelectedTeamId] = useState<string>("");

  // Resolve effective theme
  const isDark =
    theme === "dark" ||
    (theme === "system" &&
      window.matchMedia("(prefers-color-scheme: dark)").matches);

  // Fetch teams for the selector
  const { data: teams, isLoading: isLoadingTeams } = useQuery<Team[]>({
    queryKey: ["myTeams"],
    queryFn: getMyTeams,
  });

  // Automatically select the first team when teams load
  useEffect(() => {
    if (teams && teams.length > 0 && !selectedTeamId) {
      setSelectedTeamId(teams[0].id);
    }
  }, [teams, selectedTeamId]);

  // Fetch metrics for the selected team
  const {
    data: metrics,
    isLoading: isLoadingMetrics,
    error,
  } = useQuery<TeamDashboardMetrics>({
    queryKey: ["teamDashboardMetrics", selectedTeamId],
    queryFn: () => getTeamMetrics(selectedTeamId),
    enabled: selectedTeamId !== "", // Only run query if a team is selected
    refetchInterval: 30000,
  });

  if (isLoadingTeams) {
    return (
      <div className="flex h-full items-center justify-center">
        <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
      </div>
    );
  }

  if (!teams || teams.length === 0) {
    return (
      <div className="p-8">
        <div className="text-center py-12 border rounded-lg bg-muted/20">
          <Users className="h-12 w-12 mx-auto text-muted-foreground mb-4" />
          <h3 className="text-lg font-medium">No Teams Found</h3>
          <p className="text-muted-foreground mt-1">
            You are not a member of any teams in this organization.
          </p>
        </div>
      </div>
    );
  }

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-col sm:flex-row sm:items-end sm:justify-between gap-6">
        <div className="flex flex-col gap-1">
          <PageTitle>Team Overview</PageTitle>
          <p className="text-muted-foreground">
            Cross-project health and stability metrics for your team.
          </p>
        </div>

        <div className="w-full sm:w-[250px]">
          <Select
            value={selectedTeamId || ""}
            onValueChange={(value) => setSelectedTeamId(value)}
          >
            <SelectTrigger>
              <SelectValue placeholder="Select a team" />
            </SelectTrigger>
            <SelectContent>
              {teams.map((team) => (
                <SelectItem key={team.id} value={team.id}>
                  {team.name}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
      </div>

      {error ? (
        <div className="bg-destructive/15 text-destructive p-4 rounded-md border border-destructive/20 flex gap-2 items-center">
          <AlertCircle className="h-4 w-4" />
          <div>
            <p className="font-semibold">Error</p>
            <p className="text-sm">
              Failed to load team metrics. {(error as Error).message}
            </p>
          </div>
        </div>
      ) : isLoadingMetrics ? (
        <div className="flex h-64 items-center justify-center">
          <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
        </div>
      ) : !metrics ? null : (
        <>
          <div className="grid gap-6 grid-cols-1 lg:grid-cols-2">
            {/* Project Breakdown (Left Column) */}
            <Card className="col-span-1">
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <LayoutDashboard className="h-5 w-5" />
                  Project Health
                </CardTitle>
                <CardDescription>
                  Error volumes and active clusters across{" "}
                  {metrics.projectBreakdown.length} projects.
                </CardDescription>
              </CardHeader>
              <CardContent>
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Project</TableHead>
                      <TableHead className="text-right">
                        Monthly Volume
                      </TableHead>
                      <TableHead className="text-right">
                        Active Clusters
                      </TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {metrics.projectBreakdown.length > 0 ? (
                      metrics.projectBreakdown.map((project) => (
                        <TableRow key={project.projectId}>
                          <TableCell className="font-medium">
                            {project.projectName}
                          </TableCell>
                          <TableCell className="text-right">
                            {project.reportVolume.toLocaleString()}
                          </TableCell>
                          <TableCell className="text-right">
                            <Badge
                              variant={
                                project.activeClusters > 10
                                  ? "critical"
                                  : "neutral"
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
                          No projects found for this team.
                        </TableCell>
                      </TableRow>
                    )}
                  </TableBody>
                </Table>
              </CardContent>
            </Card>

            {/* Top Clusters (Right Column) */}
            <div className="col-span-1">
              {/* HottestClustersList component handles its own card wrapper, so we just pass data */}
              <HottestClustersList clusters={metrics.topClusters} />
            </div>
          </div>

          <div className="grid gap-6 grid-cols-1">
            {/* Triage Funnel (Full Width below) */}
            <Card>
              <CardHeader>
                <CardTitle>Aggregation History (24h)</CardTitle>
                <CardDescription>
                  Signal vs noise across all team projects.
                </CardDescription>
              </CardHeader>
              <CardContent>
                <TriageFunnelChart
                  data={metrics.volumeHistory}
                  noiseColor={isDark ? "#374151" : "#E5E7EB"}
                  signalColor="#3B82F6"
                />
              </CardContent>
            </Card>
          </div>
        </>
      )}
    </div>
  );
}
