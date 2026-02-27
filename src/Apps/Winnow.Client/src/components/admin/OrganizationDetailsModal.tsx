import { useEffect, useState } from "react"
import { getOrganizationDetails, type ProjectQuotaSummary } from "@/lib/api"
import {
    Dialog,
    DialogContent,
    DialogHeader,
    DialogTitle,
} from "@/components/ui/dialog"
import { Badge } from "@/components/ui/badge"
import { formatTimeAgo } from "@/lib/utils"
import { AlertCircle, Building2, Ticket, CheckCircle2 } from "lucide-react"
import { PieChart, Pie, Cell, ResponsiveContainer, Tooltip as RechartsTooltip } from 'recharts'
import { Progress } from "@/components/ui/progress"

const COLORS = ['#ef4444', '#f97316', '#eab308', '#22c55e', '#0ea5e9', '#6366f1', '#a855f7', '#ec4899'];

interface OrganizationDetailsModalProps {
    organizationId: string | null
    isOpen: boolean
    onOpenChange: (open: boolean) => void
}

export function OrganizationDetailsModal({ organizationId, isOpen, onOpenChange }: OrganizationDetailsModalProps) {
    const [isLoading, setIsLoading] = useState(false)
    const [details, setDetails] = useState<any>(null)

    useEffect(() => {
        if (isOpen && organizationId) {
            fetchDetails(organizationId)
        } else {
            setDetails(null)
        }
    }, [isOpen, organizationId])

    const fetchDetails = async (id: string) => {
        setIsLoading(true)
        try {
            const data = await getOrganizationDetails(id)
            setDetails(data)
        } catch (error) {
            console.error("Failed to fetch organization details:", error)
        } finally {
            setIsLoading(false)
        }
    }

    if (!isOpen) return null

    return (
        <Dialog open={isOpen} onOpenChange={onOpenChange}>
            <DialogContent className="max-w-3xl max-h-[90vh] overflow-y-auto border-red-900/50 bg-background">
                <DialogHeader>
                    <DialogTitle className="text-2xl font-bold flex items-center gap-2 text-foreground">
                        <Building2 className="text-red-500 h-6 w-6" />
                        {details?.name || "Loading..."}
                    </DialogTitle>
                </DialogHeader>

                {isLoading ? (
                    <div className="py-12 flex justify-center text-muted-foreground">
                        Loading organization details...
                    </div>
                ) : details ? (
                    <div className="space-y-6 mt-4">
                        {/* Summary Cards */}
                        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                            <div className="border border-red-900/30 bg-red-950/10 rounded-lg p-4">
                                <h3 className="text-sm font-medium text-muted-foreground mb-1">Total Reports</h3>
                                <p className="text-2xl font-bold text-foreground">{details.reportCount}</p>
                            </div>
                            <div className="border border-red-900/30 bg-red-950/10 rounded-lg p-4">
                                <h3 className="text-sm font-medium text-muted-foreground mb-1">Members</h3>
                                <p className="text-2xl font-bold text-foreground">{details.memberCount}</p>
                            </div>
                            <div className="border border-red-900/30 bg-red-950/10 rounded-lg p-4">
                                <h3 className="text-sm font-medium text-muted-foreground mb-1">Projects</h3>
                                <p className="text-2xl font-bold text-foreground">{details.projectCount}</p>
                            </div>
                        </div>

                        {/* Quotas Section */}
                        {details.quota && (
                            <div className="border border-red-900/30 rounded-lg overflow-hidden">
                                <div className="bg-red-950/20 px-4 py-3 border-b border-red-900/30 flex justify-between items-center">
                                    <h3 className="font-semibold text-red-400 flex items-center gap-2">
                                        <Ticket className="h-4 w-4" />
                                        Current Month Quota Usage
                                    </h3>
                                    {details.quota.isLocked ? (
                                        <Badge variant="destructive" className="bg-red-600">Locked</Badge>
                                    ) : details.quota.isOverage ? (
                                        <Badge variant="outline" className="text-orange-500 border-orange-500/50 bg-orange-500/10">Overage</Badge>
                                    ) : (
                                        <Badge variant="outline" className="text-green-500 border-green-500/50 bg-green-500/10 flex items-center gap-1">
                                            <CheckCircle2 className="h-3 w-3" /> Healthy
                                        </Badge>
                                    )}
                                </div>
                                <div className="p-4 bg-background">
                                    <div className="flex flex-col gap-2 mb-4">
                                        <div className="flex justify-between items-end">
                                            <div>
                                                <p className="text-xl font-mono text-foreground font-bold">{details.quota.monthlyReportCount} <span className="text-sm text-muted-foreground font-sans font-normal">reports used</span></p>
                                            </div>
                                            <div className="text-right">
                                                <p className="text-xs text-muted-foreground uppercase tracking-wider mb-1">Base Limit</p>
                                                <p className="text-lg font-mono text-foreground">
                                                    {details.quota.baseLimit === 2147483647 ? "Unlimited" : details.quota.baseLimit}
                                                </p>
                                            </div>
                                        </div>
                                        {details.quota.baseLimit !== 2147483647 && (
                                            <Progress
                                                value={Math.min(100, Math.round((details.quota.monthlyReportCount / details.quota.baseLimit) * 100))}
                                                className="h-3"
                                                indicatorColor={details.quota.isLocked ? "bg-red-500" : details.quota.isOverage ? "bg-orange-500" : "bg-blue-500"}
                                            />
                                        )}

                                        <div className="mt-2 text-xs text-muted-foreground text-right border-t border-red-900/20 pt-2 flex justify-between">
                                            <span>
                                                {details.quota.baseLimit !== 2147483647 &&
                                                    `${Math.min(100, Math.round((details.quota.monthlyReportCount / details.quota.baseLimit) * 100))}% of base limit used.`
                                                }
                                            </span>
                                            <span>Grace Limit: <strong className="text-foreground">{details.quota.graceLimit === 2147483647 ? "Unlimited" : details.quota.graceLimit}</strong></span>
                                        </div>
                                    </div>

                                    {details.quota.isLocked && (
                                        <div className="mt-4 p-3 bg-red-500/10 border border-red-500/30 rounded flex items-start gap-3">
                                            <AlertCircle className="h-5 w-5 text-red-500 shrink-0 mt-0.5" />
                                            <p className="text-sm text-red-200">
                                                This tenant has exceeded their grace limit. New incoming reports will be actively locked out and held for ransom.
                                            </p>
                                        </div>
                                    )}
                                </div>
                            </div>
                        )}

                        {/* Projects Breakdown */}
                        {details.projectQuotas && details.projectQuotas.length > 0 && (
                            <div className="border border-red-900/30 rounded-lg overflow-hidden shadow-lg">
                                <div className="bg-red-950/20 px-4 py-3 border-b border-red-900/30">
                                    <h3 className="font-semibold text-red-400">Monthly Usage by Project</h3>
                                </div>
                                <div className="p-4 grid grid-cols-1 md:grid-cols-2 gap-6 items-center bg-background">
                                    <div className="h-[250px] w-full">
                                        <ResponsiveContainer width="100%" height="100%">
                                            <PieChart>
                                                <Pie
                                                    data={details.projectQuotas}
                                                    cx="50%"
                                                    cy="50%"
                                                    innerRadius={60}
                                                    outerRadius={80}
                                                    paddingAngle={5}
                                                    dataKey="monthlyReportCount"
                                                    nameKey="name"
                                                >
                                                    {details.projectQuotas.map((_: ProjectQuotaSummary, index: number) => (
                                                        <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />
                                                    ))}
                                                </Pie>
                                                <RechartsTooltip
                                                    contentStyle={{ backgroundColor: '#1e0f11', borderColor: '#451a1e', borderRadius: '8px' }}
                                                    itemStyle={{ color: '#fff' }}
                                                />
                                            </PieChart>
                                        </ResponsiveContainer>
                                    </div>
                                    <div className="divide-y divide-red-900/20 max-h-[250px] overflow-y-auto pr-2">
                                        {details.projectQuotas.map((p: ProjectQuotaSummary, index: number) => (
                                            <div key={p.id} className="flex justify-between items-center py-3">
                                                <div className="flex items-center gap-3">
                                                    <div className="w-3 h-3 rounded-full mt-1 shrink-0" style={{ backgroundColor: COLORS[index % COLORS.length] }}></div>
                                                    <div>
                                                        <p className="font-medium text-sm text-foreground">{p.name}</p>
                                                        <p className="text-xs text-muted-foreground font-mono">{p.id.substring(0, 8)}...</p>
                                                    </div>
                                                </div>
                                                <div className="text-right">
                                                    <Badge variant="secondary" className="font-mono bg-background border-red-900/30 text-foreground">
                                                        {p.monthlyReportCount}
                                                    </Badge>
                                                </div>
                                            </div>
                                        ))}
                                    </div>
                                </div>
                            </div>
                        )}

                        {/* General Info */}
                        <div className="grid grid-cols-2 gap-4 text-sm mt-4 p-4 border border-red-900/30 bg-red-950/5 rounded-lg">
                            <div>
                                <span className="text-muted-foreground">ID:</span>
                                <p className="font-mono text-xs mt-1 text-foreground/80">{details.id}</p>
                            </div>
                            <div>
                                <span className="text-muted-foreground">Created:</span>
                                <p className="mt-1">{formatTimeAgo(details.createdAt)}</p>
                            </div>
                            {details.lastReportDate && (
                                <div>
                                    <span className="text-muted-foreground">Last Report:</span>
                                    <p className="mt-1">{formatTimeAgo(details.lastReportDate)}</p>
                                </div>
                            )}
                            {details.stripeCustomerId && (
                                <div>
                                    <span className="text-muted-foreground">Stripe ID:</span>
                                    <p className="font-mono text-xs mt-1 text-foreground/80">{details.stripeCustomerId}</p>
                                </div>
                            )}
                        </div>
                    </div>
                ) : (
                    <div className="py-12 flex justify-center text-muted-foreground text-red-400">
                        Failed to load details.
                    </div>
                )}
            </DialogContent>
        </Dialog>
    )
}
