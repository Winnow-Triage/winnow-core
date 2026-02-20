import { useEffect, useState } from "react"
import { getAllOrganizations, updateOrganizationSubscription, updateOrganizationStatus, deleteOrganization, type OrganizationSummary } from "@/lib/api"
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
    DropdownMenuSeparator,
} from "@/components/ui/dropdown-menu"
import {
    AlertDialog,
    AlertDialogAction,
    AlertDialogCancel,
    AlertDialogContent,
    AlertDialogDescription,
    AlertDialogFooter,
    AlertDialogHeader,
    AlertDialogTitle,
} from "@/components/ui/alert-dialog"
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

    // Deletion Modal State
    const [isDeleteDialogOpen, setIsDeleteDialogOpen] = useState(false)
    const [orgToDelete, setOrgToDelete] = useState<OrganizationSummary | null>(null)
    const [deleteConfirmationText, setDeleteConfirmationText] = useState("")
    const [isDeleting, setIsDeleting] = useState(false)

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

    const handleToggleSuspension = async (org: OrganizationSummary) => {
        const action = org.isSuspended ? "activated" : "suspended"
        try {
            await updateOrganizationStatus(org.id, !org.isSuspended)
            toast.success(`Organization successfully ${action}`)
            fetchOrganizations()
        } catch (error) {
            console.error(`Failed to ${action} organization:`, error)
            toast.error(`Failed to modify organization status`)
        }
    }

    const handleInitiateDelete = (org: OrganizationSummary) => {
        setOrgToDelete(org)
        setDeleteConfirmationText("")
        setIsDeleteDialogOpen(true)
    }

    const handleConfirmDelete = async () => {
        if (!orgToDelete || deleteConfirmationText !== orgToDelete.name) return

        try {
            setIsDeleting(true)
            await deleteOrganization(orgToDelete.id)
            toast.success("Organization permanently deleted")
            setIsDeleteDialogOpen(false)
            setOrgToDelete(null)
            fetchOrganizations()
        } catch (error) {
            console.error("Failed to delete organization:", error)
            toast.error("Failed to delete organization")
        } finally {
            setIsDeleting(false)
        }
    }

    const getTierBadgeColor = (tier: string) => {
        const t = tier.toLowerCase()
        if (t === 'starter') return 'bg-blue-500/10 text-blue-500 border-blue-500/20 hover:bg-blue-500/20'
        if (t === 'pro') return 'bg-indigo-500/10 text-indigo-500 border-indigo-500/20 hover:bg-indigo-500/20'
        if (t === 'dedicated') return 'bg-slate-500/10 text-slate-400 border-slate-500/20 hover:bg-slate-500/20'
        return 'bg-muted text-foreground hover:bg-muted/80' // Free
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
                            <SelectItem value="Starter">Starter</SelectItem>
                            <SelectItem value="Pro">Pro</SelectItem>
                            <SelectItem value="Dedicated">Dedicated</SelectItem>
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
                                        {org.isSuspended ? (
                                            <Badge className="bg-orange-500/10 text-orange-500 border-orange-500/20 hover:bg-orange-500/20 text-xs px-2 py-0" variant="outline">
                                                Suspended
                                            </Badge>
                                        ) : (
                                            <Badge className="bg-green-500/10 text-green-500 border-green-500/20 hover:bg-green-500/20 text-xs px-2 py-0" variant="outline">
                                                Active
                                            </Badge>
                                        )}
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

                                                <DropdownMenuItem
                                                    onClick={() => handleToggleSuspension(org)}
                                                    className="hover:bg-red-500/20 focus:bg-red-500/20 cursor-pointer"
                                                >
                                                    {org.isSuspended ? "Activate Tenant" : "Suspend Tenant"}
                                                </DropdownMenuItem>

                                                <DropdownMenuSeparator className="bg-red-900/30" />

                                                <div className="text-xs text-muted-foreground px-2 py-1.5 font-semibold">Subscription</div>
                                                <DropdownMenuItem onClick={() => handleUpdateTier(org.id, 'Free')} className="hover:bg-red-500/20 focus:bg-red-500/20 cursor-pointer">
                                                    Set to Free
                                                </DropdownMenuItem>
                                                <DropdownMenuItem onClick={() => handleUpdateTier(org.id, 'Starter')} className="hover:bg-blue-500/20 hover:text-blue-500 focus:bg-blue-500/20 cursor-pointer">
                                                    Set to Starter
                                                </DropdownMenuItem>
                                                <DropdownMenuItem onClick={() => handleUpdateTier(org.id, 'Pro')} className="hover:bg-indigo-500/20 hover:text-indigo-500 focus:bg-indigo-500/20 cursor-pointer">
                                                    Set to Pro
                                                </DropdownMenuItem>
                                                <DropdownMenuItem onClick={() => handleUpdateTier(org.id, 'Dedicated')} className="hover:bg-slate-500/20 hover:text-slate-400 focus:bg-slate-500/20 cursor-pointer">
                                                    Set to Dedicated
                                                </DropdownMenuItem>

                                                <DropdownMenuSeparator className="bg-red-900/30" />

                                                <DropdownMenuItem
                                                    onClick={() => handleInitiateDelete(org)}
                                                    className="text-red-500 hover:bg-red-500/20 hover:text-red-500 focus:bg-red-500/20 focus:text-red-500 cursor-pointer font-bold"
                                                >
                                                    Delete Tenant
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

            <AlertDialog open={isDeleteDialogOpen} onOpenChange={setIsDeleteDialogOpen}>
                <AlertDialogContent className="border-red-900/50 bg-background">
                    <AlertDialogHeader>
                        <AlertDialogTitle className="text-red-500 text-xl font-bold flex items-center gap-2">
                            Permanently Delete Tenant?
                        </AlertDialogTitle>
                        <AlertDialogDescription className="text-muted-foreground pt-2">
                            This action is completely destructive and cannot be undone. It will cascade delete all associated Teams, Projects, Reports, Vector Embeddings, and attempt to purge all S3 assets.
                        </AlertDialogDescription>
                    </AlertDialogHeader>

                    {orgToDelete && (
                        <div className="my-4 space-y-3 p-4 bg-red-950/20 border border-red-900/50 rounded-md">
                            <p className="text-sm">
                                Please type <strong className="text-red-400 font-mono select-all">{orgToDelete.name}</strong> to confirm.
                            </p>
                            <Input
                                value={deleteConfirmationText}
                                onChange={(e) => setDeleteConfirmationText(e.target.value)}
                                className="bg-background border-red-900/50 focus-visible:ring-red-500 font-mono"
                                placeholder={orgToDelete.name}
                            />
                        </div>
                    )}

                    <AlertDialogFooter>
                        <AlertDialogCancel className="border-red-900/50 hover:bg-red-950/20">Cancel</AlertDialogCancel>
                        <AlertDialogAction
                            onClick={(e) => {
                                e.preventDefault();
                                handleConfirmDelete();
                            }}
                            disabled={!orgToDelete || deleteConfirmationText !== orgToDelete.name || isDeleting}
                            className="bg-red-600 hover:bg-red-700 text-white font-bold disabled:opacity-50"
                        >
                            {isDeleting ? "Deleting..." : "Permanently Delete"}
                        </AlertDialogAction>
                    </AlertDialogFooter>
                </AlertDialogContent>
            </AlertDialog>
        </div >
    )
}
