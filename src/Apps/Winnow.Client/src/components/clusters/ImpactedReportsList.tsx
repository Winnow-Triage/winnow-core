import { Link } from "react-router-dom";
import { Clock } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { formatTimeAgo } from "@/lib/utils";

interface ClusterMember {
  id: string;
  title: string;
  message: string;
  status: string;
  createdAt: string;
  confidenceScore?: number;
}

interface ImpactedReportsListProps {
  reports: ClusterMember[];
}

export function ImpactedReportsList({ reports }: ImpactedReportsListProps) {
  return (
    <Card className="border border-white/5 dark:border-white/10 shadow-2xl bg-white/5 dark:bg-white/[0.02] backdrop-blur-sm rounded-3xl overflow-hidden">
      <CardHeader className="p-6 pb-2">
        <CardTitle className="text-xl font-bold flex items-center gap-3 tracking-tight">
          <div className="p-2 bg-blue-500/10 rounded-xl border border-blue-500/20 shadow-lg shadow-blue-500/5">
            <Clock className="h-5 w-5 text-blue-500" />
          </div>
          Impacted Reports
          <Badge
            variant="secondary"
            className="ml-auto rounded-full px-3"
          >
            {reports.length}
          </Badge>
        </CardTitle>
      </CardHeader>
      <CardContent className="p-4">
        <div className="flex flex-col gap-3 overflow-y-auto max-h-[520px] pr-1">
          {reports.map((report) => (
            <Link
              key={report.id}
              to={`/reports/${report.id}`}
              className="group shrink-0 p-4 bg-white/5 border border-white/5 hover:border-blue-500/30 hover:bg-white/10 rounded-2xl transition-all duration-300 shadow-sm relative overflow-hidden"
            >
              {/* Confidence Progress Bar */}
              {report.confidenceScore && (
                <div
                  className="absolute bottom-0 left-0 h-[2px] bg-blue-500 transition-all group-hover:h-1"
                  style={{
                    width: `${report.confidenceScore * 100}%`,
                    opacity: 0.5,
                  }}
                />
              )}

              <div className="flex flex-col gap-2 relative z-10">
                <div className="flex items-start justify-between gap-2">
                  <span className="font-bold text-sm tracking-tight group-hover:text-blue-400 transition-colors line-clamp-2">
                    {report.title || report.message}
                  </span>
                  <Badge
                    variant="outline"
                    className="text-[9px] h-fit px-1.5 py-0 bg-white/5 border-white/10 opacity-60"
                  >
                    {report.status}
                  </Badge>
                </div>

                <div className="flex items-center justify-between mt-1 text-[11px]">
                  <span className="text-muted-foreground opacity-70">
                    {formatTimeAgo(report.createdAt)}
                  </span>
                  {report.confidenceScore && (
                    <span className="text-blue-500 font-black tracking-tighter">
                      {(report.confidenceScore * 100).toFixed(0)}% MATCH
                    </span>
                  )}
                </div>
              </div>
            </Link>
          ))}
        </div>
      </CardContent>
    </Card>
  );
}
