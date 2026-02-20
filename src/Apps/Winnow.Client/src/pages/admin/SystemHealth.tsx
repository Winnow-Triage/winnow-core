import { useEffect, useState } from "react";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { getSystemHealth, type SystemHealthResponse } from "@/lib/api";
import { toast } from "sonner";
import { Loader2 } from "lucide-react";
import { cn } from "@/lib/utils";

import ReactJson from "@microlink/react-json-view";

export default function SystemHealth() {
    const [health, setHealth] = useState<SystemHealthResponse | null>(null);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        const fetchHealth = async () => {
            try {
                const data = await getSystemHealth();
                setHealth(data);
            } catch (error) {
                console.error("Failed to fetch system health:", error);
                toast.error("Failed to fetch system health");
            } finally {
                setLoading(false);
            }
        };

        fetchHealth();
        // Poll every 30 seconds
        const interval = setInterval(fetchHealth, 30000);
        return () => clearInterval(interval);
    }, []);

    if (loading && !health) {
        return (
            <div className="flex items-center justify-center h-[50vh]">
                <Loader2 className="h-8 w-8 animate-spin text-red-500" />
            </div>
        );
    }

    return (
        <div className="flex flex-col gap-6 w-full max-w-6xl mx-auto py-8">
            <div className="flex items-center justify-between">
                <div>
                    <h1 className="text-3xl font-bold tracking-tight text-red-500">System Health</h1>
                    <p className="text-muted-foreground mt-2">
                        Monitor the status of core services and external integrations.
                    </p>
                </div>
                {health && (
                    <Badge variant="outline" className={cn(
                        "text-sm py-1 px-3",
                        health.status === "Healthy" ? "bg-green-500/10 text-green-500 border-green-500/20" : "bg-red-500/10 text-red-500 border-red-500/20"
                    )}>
                        System: {health.status}
                    </Badge>
                )}
            </div>

            <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
                {health?.checks.map((check) => (
                    <Card key={check.name} className="border-red-500/20 bg-background/50">
                        <CardHeader className="pb-2">
                            <CardTitle className="text-sm font-medium text-muted-foreground text-red-400">{check.name}</CardTitle>
                        </CardHeader>
                        <CardContent>
                            <div className="flex items-center justify-between">
                                <div className="text-lg font-bold truncate pr-4 text-muted-foreground/80">
                                    {check.description && check.description !== "Healthy" && check.description !== "Ready" ? check.description : "Operational"}
                                </div>
                                <Badge variant="outline" className={cn(
                                    check.status === "Healthy" ? "bg-green-500/10 text-green-500 border-green-500/20" : "bg-red-500/10 text-red-500 border-red-500/20"
                                )}>
                                    {check.status}
                                </Badge>
                            </div>
                            <div className="text-xs text-muted-foreground mt-2">Response time: {check.duration}</div>
                        </CardContent>
                    </Card>
                ))}
            </div>

            <Card className="border-red-500/20 bg-background/50 mt-4">
                <CardHeader>
                    <CardTitle className="text-red-400">Health Endpoint Diagnostics</CardTitle>
                    <CardDescription>Live diagnostics from the /health/detailed endpoint.</CardDescription>
                </CardHeader>
                <CardContent>
                    <div className="text-sm font-mono bg-muted/50 p-4 rounded-md overflow-x-auto">
                        {health && (
                            <ReactJson
                                src={health}
                                theme="bespin"
                                name={false}
                                displayDataTypes={false}
                                displayObjectSize={false}
                                enableClipboard={true}
                                collapsed={2}
                                style={{ backgroundColor: 'transparent' }}
                            />
                        )}
                    </div>
                </CardContent>
            </Card>
        </div>
    );
}
