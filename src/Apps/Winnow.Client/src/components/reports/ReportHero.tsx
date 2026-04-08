import { ArrowLeft, ExternalLink } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { PageTitle } from "@/components/ui/page-title";
import { useNavigate } from "react-router-dom";

interface ReportHeroProps {
  report: {
    id: string;
    title: string;
    status: string;
    assignedTo?: string;
    externalUrl?: string;
  };
}

export function ReportHero({ report }: ReportHeroProps) {
  const navigate = useNavigate();

  return (
    <div className="flex items-center gap-4">
      <Button variant="ghost" size="icon" onClick={() => navigate(-1)}>
        <ArrowLeft className="h-4 w-4" />
      </Button>
      <div className="flex flex-col gap-1 flex-1">
        <div className="flex items-center gap-2">
          <span className="text-xs font-mono text-muted-foreground bg-muted/50 px-2 py-0.5 rounded-md">
            R-{report.id.substring(0, 8)}
          </span>
          <Badge
            variant="neutral"
            className="h-5 px-2 text-[10px] uppercase tracking-wider"
          >
            {report.status}
          </Badge>
          {report.assignedTo && (
            <Badge
              variant="outline"
              className="h-5 px-2 text-[10px] bg-blue-500/5 text-blue-600 border-blue-200 dark:border-blue-900"
            >
              {report.assignedTo}
            </Badge>
          )}
        </div>
        <PageTitle className="text-3xl md:text-4xl">{report.title}</PageTitle>
      </div>
      <div className="ml-auto flex items-center gap-2">
        {/* External Link Button - Only visible if URL exists */}
        {report.externalUrl && (
          <Button variant="outline" asChild>
            <a
              href={report.externalUrl}
              target="_blank"
              rel="noopener noreferrer"
            >
              <ExternalLink className="mr-2 h-4 w-4" />
              Open in External System
            </a>
          </Button>
        )}
      </div>
    </div>
  );
}
