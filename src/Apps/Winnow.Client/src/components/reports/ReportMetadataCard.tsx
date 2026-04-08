import React from "react";
import { Clock, AlertCircle, MessageSquare, Sparkles } from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Separator } from "@/components/ui/separator";

interface ReportMetadataCardProps {
  report: {
    createdAt: string;
    metadata?: string;
  };
}

export function ReportMetadataCard({ report }: ReportMetadataCardProps) {
  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-sm font-medium">Metadata</CardTitle>
      </CardHeader>
      <CardContent className="flex flex-col gap-4 text-sm">
        <div className="flex justify-between">
          <span className="text-muted-foreground flex items-center gap-2">
            <Clock className="h-3 w-3" /> Created
          </span>
          <span className="text-right">
            {new Date(report.createdAt).toLocaleString()}
          </span>
        </div>
        <Separator />
        <div className="flex justify-between">
          <span className="text-muted-foreground flex items-center gap-2">
            <AlertCircle className="h-3 w-3" /> Priority
          </span>
          <span>Normal</span>
        </div>
        <Separator />
        <div className="flex justify-between">
          <span className="text-muted-foreground flex items-center gap-2">
            <MessageSquare className="h-3 w-3" /> Comments
          </span>
          <span>0</span>
        </div>
        {report.metadata && (
          <>
            <Separator />
            <div className="flex flex-col gap-2">
              <span className="text-muted-foreground flex items-center gap-2">
                <Sparkles className="h-3 w-3" /> Session Context
              </span>
              <div className="bg-muted/50 p-2 rounded-md grid grid-cols-2 gap-x-4 gap-y-1 overflow-hidden">
                {(() => {
                  try {
                    const metadata = JSON.parse(report.metadata);
                    return Object.entries(metadata)
                      .filter(
                        ([key]) => key !== "logs" && key !== "context",
                      )
                      .map(([key, value]) => (
                        <React.Fragment key={key}>
                          <span className="text-[10px] font-medium text-muted-foreground uppercase truncate" title={key}>
                            {key}
                          </span>
                          <span
                            className="text-[10px] text-right font-mono truncate"
                            title={String(value)}
                          >
                            {String(value)}
                          </span>
                        </React.Fragment>
                      ));
                  } catch {
                    return (
                      <span className="text-xs text-red-500 col-span-2">
                        Error parsing metadata
                      </span>
                    );
                  }
                })()}
              </div>
            </div>
          </>
        )}
      </CardContent>
    </Card>
  );
}
