import { useEffect, useState } from "react"
import { getAllOrganizations, updateOrganizationSubscription, type OrganizationSummary } from "@/lib/api"
import {
    Table,
    TableBody,
    TableCell,
    TableHead,
    TableHeader,
    TableRow,
} from "@/components/ui/table"
import {
    DropdownMenu,
    DropdownMenuContent,
    DropdownMenuItem,
    DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import { MoreHorizontal } from "lucide-react"
import { formatTimeAgo } from "@/lib/utils"
import { toast } from "sonner"

import { Input } from "@/components/ui/input"
import {
    Select,
    SelectContent,
    SelectItem,
    SelectTrigger,
    SelectValue,
} from "@/components/ui/select"

export default function OrganizationsDashboard() {
    const [organizations, setOrganizations] = useState<OrganizationSummary[]>([])
    const [isLoading, setIsLoading] = useState(true)
    const [searchQuery, setSearchQuery] = useState("")
    const [tierFilter, setTierFilter] = useState<string>("All")

    const fetchOrganizations = async () => {
        try {
            setIsLoading(true)
            const data = await getAllOrganizations()
            setOrganizations(data)
        } catch (error) {
            console.error("Failed to fetch organizations:", error)
            toast.error("Failed to load organizations")
        } finally {
            setIsLoading(false)
        }
    }

    useEffect(() => {
        fetchOrganizations()
    }, [])

    const handleUpdateTier = async (id: string, newTier: string) => {
        try {
            await updateOrganizationSubscription(id, newTier)
            toast.success("Subscription updated successfully")
            fetchOrganizations()
        } catch (error) {
            console.error("Failed to update subscription:", error)
            toast.error("Failed to update subscription")
        }
    }

    const handleImpersonate = (orgName: string) => {
        toast.info(`Impersonating tenant: ${orgName}`, {
            description: "This feature allows you to securely view the system exactly as this tenant sees it.",
        });
        // In a real implementation, this would trigger an API call to get an impersonation token
        // and redirect the super admin to the tenant dashboard with that context.
    }

    const getTierBadgeColor = (tier: string) => {
        const t = tier.toLowerCase()
        if (t === 'pro') return 'bg-blue-500/10 text-blue-500 hover:bg-blue-500/20'
        if (t === 'enterprise') return 'bg-purple-500/10 text-purple-500 hover:bg-purple-500/20'
        return 'bg-muted text-muted-foreground hover:bg-muted/80'
    }

    // Filter organizations based on search query and selected tier
    const filteredOrganizations = organizations.filter(org => {
        const matchesSearch = org.name.toLowerCase().includes(searchQuery.toLowerCase());
        const matchesTier = tierFilter === "All" || org.subscriptionTier === tierFilter;
        return matchesSearch && matchesTier;
    });

    return (
        <div className="space-y-6">
            <div>
                <h1 className="text-3xl font-bold tracking-tight text-red-500">Tenant Organizations</h1>
                <p className="text-muted-foreground">Manage all tenants across the system.</p>
            </div>

            <div className="flex flex-col sm:flex-row gap-4 items-center justify-between">
                <div className="flex w-full sm:max-w-md items-center space-x-2">
                    <Input
                        placeholder="Search organizations..."
                        value={searchQuery}
                        onChange={(e) => setSearchQuery(e.target.value)}
                        className="bg-background/50 border-red-900/50 focus-visible:ring-red-500"
                    />
                </div>
                <div className="w-full sm:w-auto">
                    <Select value={tierFilter} onValueChange={setTierFilter}>
                        <SelectTrigger className="w-full sm:w-[180px] bg-background/50 border-red-900/50 focus:ring-red-500">
                            <SelectValue placeholder="Filter by Tier" />
                        </SelectTrigger>
                        <SelectContent className="border-red-900/50">
                            <SelectItem value="All">All Tiers</SelectItem>
                            <SelectItem value="Free">Free</SelectItem>
                            <SelectItem value="Pro">Pro</SelectItem>
                            <SelectItem value="Enterprise">Enterprise</SelectItem>
                        </SelectContent>
                    </Select>
                </div>
            </div>

            <div className="rounded-md border border-red-900/50">
                <Table>
                    <TableHeader className="bg-red-950/20">
                        <TableRow className="border-red-900/50">
                            <TableHead className="text-red-400">Organization Name</TableHead>
                            <TableHead className="text-red-400">Status</TableHead>
                            <TableHead className="text-red-400">Members</TableHead>
                            <TableHead className="text-red-400">Teams</TableHead>
                            <TableHead className="text-red-400">Projects</TableHead>
                            <TableHead className="text-red-400">Subscription</TableHead>
                            <TableHead className="text-red-400">Created</TableHead>
                            <TableHead className="text-right text-red-400">Actions</TableHead>
                        </TableRow>
                    </TableHeader>
                    <TableBody>
                        {isLoading ? (
                            <TableRow>
                                <TableCell colSpan={8} className="h-24 text-center text-muted-foreground">
                                    Loading organizations...
                                </TableCell>
                            </TableRow>
                        ) : filteredOrganizations.length === 0 ? (
                            <TableRow>
                                <TableCell colSpan={8} className="h-24 text-center text-muted-foreground">
                                    No organizations found matching your criteria.
                                </TableCell>
                            </TableRow>
                        ) : (
                            filteredOrganizations.map((org) => (
                                <TableRow key={org.id} className="border-red-900/20 hover:bg-red-950/10">
                                    <TableCell className="font-medium text-foreground">{org.name}</TableCell>
                                    <TableCell>
                                        <Badge className="bg-green-500/10 text-green-500 border-green-500/20 hover:bg-green-500/20 text-xs px-2 py-0" variant="outline">
                                            Active
                                        </Badge>
                                    </TableCell>
                                    <TableCell className="text-muted-foreground">{org.memberCount}</TableCell>
                                    <TableCell className="text-muted-foreground">{org.teamCount}</TableCell>
                                    <TableCell className="text-muted-foreground">{org.projectCount}</TableCell>
                                    <TableCell>
                                        <Badge className={getTierBadgeColor(org.subscriptionTier)} variant="outline">
                                            {org.subscriptionTier}
                                        </Badge>
                                    </TableCell>
                                    <TableCell className="text-muted-foreground whitespace-nowrap">
                                        {formatTimeAgo(org.createdAt)}
                                    </TableCell>
                                    <TableCell className="text-right">
                                        <DropdownMenu>
                                            <DropdownMenuTrigger asChild>
                                                <Button variant="ghost" className="h-8 w-8 p-0 hover:bg-red-500/20 hover:text-red-500">
                                                    <span className="sr-only">Open menu</span>
                                                    <MoreHorizontal className="h-4 w-4" />
                                                </Button>
                                            </DropdownMenuTrigger>
                                            <DropdownMenuContent align="end" className="border-red-900/50 min-w-[200px]">
                                                <DropdownMenuItem onClick={() => handleImpersonate(org.name)} className="font-medium text-orange-400 focus:text-orange-400 focus:bg-orange-500/10 mb-2">
                                                    Impersonate Tenant
                                                </DropdownMenuItem>
                                                <div className="h-px bg-red-900/30 my-1 mx-2" />
                                                <div className="text-xs text-muted-foreground px-2 py-1.5 font-semibold">Subscription</div>
                                                <DropdownMenuItem onClick={() => handleUpdateTier(org.id, 'Free')} className="hover:bg-red-500/20 focus:bg-red-500/20 cursor-pointer">
                                                    Set to Free
                                                </DropdownMenuItem>
                                                <DropdownMenuItem onClick={() => handleUpdateTier(org.id, 'Pro')} className="hover:bg-blue-500/20 hover:text-blue-500 focus:bg-blue-500/20 cursor-pointer">
                                                    Set to Pro
                                                </DropdownMenuItem>
                                                <DropdownMenuItem onClick={() => handleUpdateTier(org.id, 'Enterprise')} className="hover:bg-purple-500/20 hover:text-purple-500 focus:bg-purple-500/20 cursor-pointer">
                                                    Set to Enterprise
                                                </DropdownMenuItem>
                                            </DropdownMenuContent>
                                        </DropdownMenu>
                                    </TableCell>
                                </TableRow>
                            ))
                        )}
                    </TableBody>
                </Table>
            </div>
        </div>
    )
}
