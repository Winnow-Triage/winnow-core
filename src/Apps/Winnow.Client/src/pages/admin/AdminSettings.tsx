import { useState } from "react";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Badge } from "@/components/ui/badge";
import { toast } from "sonner";
import { getAdminReport, toggleAdminReportLock, resetAdminReportOverage, type AdminReportResponse } from "@/lib/api";
import { Search, Lock, Unlock } from "lucide-react";
import {
    Table,
    TableBody,
    TableCell,
    TableHead,
    TableHeader,
    TableRow,
} from "@/components/ui/table";

export default function AdminSettings() {
    const [reportId, setReportId] = useState("");
    const [report, setReport] = useState<AdminReportResponse | null>(null);
    const [isSearching, setIsSearching] = useState(false);
    const [isToggling, setIsToggling] = useState(false);
    const [isResetting, setIsResetting] = useState(false);

    const handleSearchReport = async () => {
        if (!reportId.trim()) return;
        try {
            setIsSearching(true);
            setReport(null);
            const data = await getAdminReport(reportId);
            setReport(data);
        } catch (error) {
            console.error(error);
            toast.error("Report not found or error fetching report.");
        } finally {
            setIsSearching(false);
        }
    };

    const handleToggleLock = async () => {
        if (!report) return;
        try {
            setIsToggling(true);
            const data = await toggleAdminReportLock(report.id);
            setReport({ ...report, isLocked: data.isLocked });
            toast.success(`Report successfully ${data.isLocked ? 'locked' : 'unlocked'}`);
        } catch (error) {
            console.error(error);
            toast.error("Failed to toggle report lock status.");
        } finally {
            setIsToggling(false);
        }
    };

    const handleResetOverage = async () => {
        if (!report) return;
        try {
            setIsResetting(true);
            const data = await resetAdminReportOverage(report.id);
            setReport({ ...report, isOverage: data.isOverage });
            toast.success("Report overage status successfully reset");
        } catch (error) {
            console.error(error);
            toast.error("Failed to reset report overage status.");
        } finally {
            setIsResetting(false);
        }
    };

    return (
        <div className="space-y-6">
            <div className="flex items-center justify-between">
                <div>
                    <h1 className="text-3xl font-bold tracking-tight text-red-500">Admin Settings</h1>
                    <p className="text-muted-foreground">Global configuration and dangerous administrative actions.</p>
                </div>
            </div>

            <Card className="border-red-500/50 bg-red-500/5 mt-4">
                <CardHeader>
                    <CardTitle className="text-red-500 flex items-center gap-2">
                        <span className="text-xl">⚠️</span> Danger Zone
                    </CardTitle>
                    <CardDescription className="text-red-400/80">
                        These actions can severely impact the system and cannot be undone easily. Proceed with extreme caution.
                    </CardDescription>
                </CardHeader>
                <CardContent className="flex flex-col gap-4">
                    <div className="flex items-center justify-between p-4 border border-red-500/20 rounded-lg bg-background/50">
                        <div>
                            <h3 className="font-semibold text-foreground">Purge Orphaned Data</h3>
                            <p className="text-sm text-muted-foreground">Permanently delete reports and vector embeddings that belong to deleted tenants.</p>
                        </div>
                        <Button variant="destructive" className="bg-red-600 hover:bg-red-700">Run Purge</Button>
                    </div>

                    <div className="flex items-center justify-between p-4 border border-red-500/20 rounded-lg bg-background/50">
                        <div>
                            <h3 className="font-semibold text-foreground">Force Sync LLM Models</h3>
                            <p className="text-sm text-muted-foreground">Clear cached capability flags and re-query the Ollama/OpenAI API for available models.</p>
                        </div>
                        <Button variant="outline" className="border-red-500/50 text-red-400 hover:bg-red-500/10 hover:text-red-300">Force Sync</Button>
                    </div>

                    <div className="flex items-center justify-between p-4 border border-red-500/20 rounded-lg bg-background/50">
                        <div>
                            <h3 className="font-semibold text-foreground">Maintenance Mode</h3>
                            <p className="text-sm text-muted-foreground">Block all non-admin traffic and API ingestion. Active users will be forcibly logged out.</p>
                        </div>
                        <Button variant="destructive" className="bg-red-600 hover:bg-red-700">Enable Maintenance Mode</Button>
                    </div>
                </CardContent>
            </Card>

            <Card className="border-blue-500/20 bg-blue-500/5 mt-4">
                <CardHeader>
                    <CardTitle className="text-blue-400">Ticket Management</CardTitle>
                    <CardDescription>Lookup and manage individual reports bypassing tenant isolation.</CardDescription>
                </CardHeader>
                <CardContent className="space-y-4">
                    <div className="flex gap-2">
                        <Input
                            placeholder="Enter Report ID (Guid)..."
                            value={reportId}
                            onChange={e => setReportId(e.target.value)}
                            onKeyDown={e => e.key === 'Enter' && handleSearchReport()}
                            className="bg-background/50 border-blue-900/50 focus-visible:ring-blue-500 font-mono"
                        />
                        <Button
                            onClick={handleSearchReport}
                            disabled={isSearching || !reportId.trim()}
                            className="bg-blue-600 hover:bg-blue-700 text-white"
                        >
                            {isSearching ? "Searching..." : <><Search className="w-4 h-4 mr-2" /> Lookup</>}
                        </Button>
                    </div>

                    {report && (
                        <div className="border border-blue-900/30 rounded-lg p-4 bg-blue-950/20 mt-4 flex justify-between items-center">
                            <div>
                                <h3 className="font-semibold text-foreground text-lg">{report.title}</h3>
                                <div className="text-sm text-muted-foreground mt-1 space-y-1">
                                    <p>Organization: <span className="font-mono text-foreground/80">{report.organizationId}</span></p>
                                    <p>Project: <span className="font-mono text-foreground/80">{report.projectId}</span></p>
                                </div>
                                <div className="mt-3 flex gap-2">
                                    <Badge variant="outline" className="border-blue-500/30 text-blue-400 bg-blue-500/10">
                                        {report.status}
                                    </Badge>
                                    {report.isLocked ? (
                                        <Badge variant="destructive" className="bg-red-600 flex gap-1 items-center">
                                            <Lock className="w-3 h-3" /> Locked
                                        </Badge>
                                    ) : (
                                        <Badge variant="outline" className="border-green-500/50 text-green-500 bg-green-500/10 flex gap-1 items-center">
                                            <Unlock className="w-3 h-3" /> Unlocked
                                        </Badge>
                                    )}
                                    {report.isOverage && (
                                        <Badge variant="destructive" className="bg-amber-600 flex gap-1 items-center">
                                            ⚠️ Overage
                                        </Badge>
                                    )}
                                </div>
                            </div>
                            <div>
                                {report.isOverage && (
                                    <Button
                                        variant="outline"
                                        className="border-amber-600/50 text-amber-500 hover:bg-amber-600/10 hover:text-amber-400 mr-2"
                                        onClick={handleResetOverage}
                                        disabled={isResetting}
                                    >
                                        {isResetting ? "Updating..." : "Reset Overage"}
                                    </Button>
                                )}
                                <Button
                                    variant={report.isLocked ? "default" : "destructive"}
                                    className={report.isLocked ? "bg-green-600 hover:bg-green-700 text-white" : "bg-red-600 hover:bg-red-700 text-white"}
                                    onClick={handleToggleLock}
                                    disabled={isToggling}
                                >
                                    {isToggling ? "Updating..." : report.isLocked ? <><Unlock className="w-4 h-4 mr-2" /> Unlock Report</> : <><Lock className="w-4 h-4 mr-2" /> Lock Report</>}
                                </Button>
                            </div>
                        </div>
                    )}
                </CardContent>
            </Card>

            <Card className="border-red-500/20 bg-background/50 mt-4">
                <CardHeader>
                    <CardTitle className="text-red-400">Audit Logs</CardTitle>
                    <CardDescription>Immutable record of privileged administrative actions.</CardDescription>
                </CardHeader>
                <CardContent>
                    <div className="rounded-md border border-red-900/50">
                        <Table>
                            <TableHeader className="bg-red-950/20">
                                <TableRow className="border-red-900/50 hover:bg-transparent">
                                    <TableHead className="text-red-400 w-[180px]">Timestamp</TableHead>
                                    <TableHead className="text-red-400 w-[200px]">Actor</TableHead>
                                    <TableHead className="text-red-400 w-[150px]">Action</TableHead>
                                    <TableHead className="text-red-400">Target Resource</TableHead>
                                </TableRow>
                            </TableHeader>
                            <TableBody>
                                <TableRow className="border-red-900/20 hover:bg-red-950/10">
                                    <TableCell className="text-muted-foreground whitespace-nowrap">2026-02-20 09:42:15 UTC</TableCell>
                                    <TableCell className="font-medium">superadmin@example.com</TableCell>
                                    <TableCell><span className="text-orange-400">UpdateTier</span></TableCell>
                                    <TableCell className="text-muted-foreground">Tenant: "Acme Corp" (Free → Pro)</TableCell>
                                </TableRow>
                                <TableRow className="border-red-900/20 hover:bg-red-950/10">
                                    <TableCell className="text-muted-foreground whitespace-nowrap">2026-02-20 09:15:02 UTC</TableCell>
                                    <TableCell className="font-medium">superadmin@example.com</TableCell>
                                    <TableCell><span className="text-red-500 font-medium">PurgeData</span></TableCell>
                                    <TableCell className="text-muted-foreground">Tenants: target_id_8f92a, target_id_11b4</TableCell>
                                </TableRow>
                                <TableRow className="border-red-900/20 hover:bg-red-950/10">
                                    <TableCell className="text-muted-foreground whitespace-nowrap">2026-02-19 14:30:00 UTC</TableCell>
                                    <TableCell className="font-medium">system</TableCell>
                                    <TableCell><span className="text-blue-400">ForceSync</span></TableCell>
                                    <TableCell className="text-muted-foreground">LLM Models Refresh</TableCell>
                                </TableRow>
                            </TableBody>
                        </Table>
                    </div>
                </CardContent>
            </Card>
        </div>
    );
}
