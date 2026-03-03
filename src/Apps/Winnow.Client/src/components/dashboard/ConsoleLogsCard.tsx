import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  CardDescription,
} from "@/components/ui/card";
import { Terminal, AlertTriangle, AlertCircle, Info } from "lucide-react";
import { ScrollArea } from "@/components/ui/scroll-area";

interface LogEntry {
  timestamp: string;
  level: "info" | "warn" | "error";
  message: string;
}

interface ConsoleLogsCardProps {
  logs: LogEntry[] | string; // Handle if it comes as string or parsed object
}

export function ConsoleLogsCard({ logs }: ConsoleLogsCardProps) {
  let parsedLogs: LogEntry[] = [];

  if (Array.isArray(logs)) {
    parsedLogs = logs;
  } else if (typeof logs === "string") {
    try {
      parsedLogs = JSON.parse(logs);
    } catch {
      return null; // Invalid logs format
    }
  }

  if (!parsedLogs || parsedLogs.length === 0) return null;

  return (
    <Card className="col-span-full">
      <CardHeader>
        <CardTitle className="flex items-center gap-2 text-sm font-medium">
          <Terminal className="h-4 w-4" />
          Console Logs
        </CardTitle>
        <CardDescription>
          Recent console activity captured before the report.
        </CardDescription>
      </CardHeader>
      <CardContent>
        <ScrollArea className="h-[200px] w-full rounded-md border bg-slate-950 p-4">
          <div className="font-mono text-xs">
            {parsedLogs.map((log, index) => (
              <div key={index} className="flex gap-2 mb-1.5 last:mb-0">
                <span className="text-slate-500 shrink-0">
                  {new Date(log.timestamp).toLocaleTimeString([], {
                    hour12: false,
                    hour: "2-digit",
                    minute: "2-digit",
                    second: "2-digit",
                    fractionalSecondDigits: 3,
                  })}
                </span>
                <span
                  className={`shrink-0 ${
                    log.level === "error"
                      ? "text-red-500"
                      : log.level === "warn"
                        ? "text-amber-500"
                        : "text-blue-400"
                  }`}
                >
                  {log.level === "error" ? (
                    <AlertCircle className="h-3 w-3 inline mr-1" />
                  ) : log.level === "warn" ? (
                    <AlertTriangle className="h-3 w-3 inline mr-1" />
                  ) : (
                    <Info className="h-3 w-3 inline mr-1" />
                  )}
                  [{log.level.toUpperCase()}]
                </span>
                <span className="text-slate-300 break-all whitespace-pre-wrap">
                  {log.message}
                </span>
              </div>
            ))}
          </div>
        </ScrollArea>
      </CardContent>
    </Card>
  );
}
