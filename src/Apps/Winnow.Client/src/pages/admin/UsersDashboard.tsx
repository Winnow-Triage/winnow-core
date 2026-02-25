import { useEffect, useState } from "react"
import { getAllUsers, adminCreateUser, toggleUserLock, impersonateUser, getAllOrganizations, addOrganizationMember, adminRemoveOrganizationMember, adminDeleteUser, type UserSummary, type OrganizationSummary } from "@/lib/api"
import {
    Table,
    TableBody,
    TableCell,
    TableHead,
    TableHeader,
    TableRow,
} from "@/components/ui/table"
import {
    Select,
    SelectContent,
    SelectItem,
    SelectTrigger,
    SelectValue,
} from "@/components/ui/select"
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
import { MoreHorizontal, Plus, ShieldCheck, Lock, Unlock, UserMinus, Building, X } from "lucide-react"
import { formatTimeAgo } from "@/lib/utils"
import { toast } from "sonner"
import { Input } from "@/components/ui/input"

export default function UsersDashboard() {
    const [users, setUsers] = useState<UserSummary[]>([])
    const [organizations, setOrganizations] = useState<OrganizationSummary[]>([])
    const [isLoading, setIsLoading] = useState(true)
    const [searchQuery, setSearchQuery] = useState("")
    const [selectedOrg, setSelectedOrg] = useState("all")
    const [selectedRole, setSelectedRole] = useState("all")
    const [selectedStatus, setSelectedStatus] = useState("all")
    const [isCreating, setIsCreating] = useState(false)
    const [isCreateDialogOpen, setIsCreateDialogOpen] = useState(false)

    // Membership Management State
    const [isMembershipDialogOpen, setIsMembershipDialogOpen] = useState(false)
    const [userToManage, setUserToManage] = useState<UserSummary | null>(null)
    const [orgToJoin, setOrgToJoin] = useState<string>("")
    const [roleToJoin, setRoleToJoin] = useState<string>("Member")
    const [isUpdatingMembership, setIsUpdatingMembership] = useState(false)

    // Delete User State
    const [isDeleteDialogOpen, setIsDeleteDialogOpen] = useState(false)
    const [userToDelete, setUserToDelete] = useState<UserSummary | null>(null)
    const [isDeleting, setIsDeleting] = useState(false)

    // New User State
    const [newUser, setNewUser] = useState({
        email: "",
        fullName: "",
        password: "",
        role: "Member"
    })

    const fetchUsers = async () => {
        try {
            setIsLoading(true)
            const [userData, orgData] = await Promise.all([
                getAllUsers(),
                getAllOrganizations()
            ])
            setUsers(userData)
            setOrganizations(orgData)
        } catch (error) {
            console.error("Failed to fetch dashboard data:", error)
            toast.error("Failed to load dashboard data")
        } finally {
            setIsLoading(false)
        }
    }

    useEffect(() => {
        fetchUsers()
    }, [])

    const handleToggleLock = async (user: UserSummary) => {
        try {
            await toggleUserLock(user.id)
            toast.success(user.isLockedOut ? "User unlocked successfully" : "User locked successfully")
            fetchUsers()
        } catch (error) {
            console.error("Failed to toggle lock:", error)
            toast.error("Failed to update user status")
        }
    }

    const handleImpersonate = async (user: UserSummary) => {
        try {
            const data = await impersonateUser(user.id)
            // Store the new token and reload the app
            localStorage.removeItem("lastProjectId")
            localStorage.setItem("authToken", data.token)
            toast.success(`Now impersonating ${user.email}`)
            setTimeout(() => {
                window.location.href = "/dashboard"
            }, 1000)
        } catch (error) {
            console.error("Failed to impersonate:", error)
            toast.error("Impersonation failed")
        }
    }

    const handleCreateUser = async () => {
        if (!newUser.email || !newUser.fullName || !newUser.password) {
            toast.error("All fields are required")
            return
        }

        try {
            setIsCreating(true)
            await adminCreateUser(newUser)
            toast.success("User created successfully")
            setIsCreateDialogOpen(false)
            setNewUser({ email: "", fullName: "", password: "", role: "Member" })
            fetchUsers()
        } catch (error) {
            console.error("Failed to create user:", error)
            toast.error("Failed to create user")
        } finally {
            setIsCreating(false)
        }
    }

    const handleAddMembership = async () => {
        if (!userToManage || !orgToJoin) return

        try {
            setIsUpdatingMembership(true)
            await addOrganizationMember(orgToJoin, roleToJoin, userToManage.id)
            toast.success("User added to organization")
            setOrgToJoin("")
            // Refresh the specific user's data or the whole list
            fetchUsers()
        } catch (error) {
            console.error("Failed to add membership:", error)
            toast.error("Failed to add user to organization")
        } finally {
            setIsUpdatingMembership(false)
        }
    }

    const handleRemoveMembership = async (orgId: string) => {
        if (!userToManage) return

        try {
            setIsUpdatingMembership(true)
            await adminRemoveOrganizationMember(orgId, userToManage.id)
            toast.success("User removed from organization")
            fetchUsers()
            // Update local state for immediate feedback if needed
            setUserToManage(prev => prev ? {
                ...prev,
                organizations: prev.organizations.filter(o => o.id !== orgId)
            } : null)
        } catch (error) {
            console.error("Failed to remove membership:", error)
            toast.error("Failed to remove user from organization")
        } finally {
            setIsUpdatingMembership(false)
        }
    }

    const handleDeleteUser = async () => {
        if (!userToDelete) return

        try {
            setIsDeleting(true)
            await adminDeleteUser(userToDelete.id)
            toast.success("User deleted successfully")
            setIsDeleteDialogOpen(false)
            setUserToDelete(null)
            fetchUsers()
        } catch (error) {
            console.error("Failed to delete user:", error)
            toast.error("Failed to delete user account")
        } finally {
            setIsDeleting(false)
        }
    }

    const filteredUsers = users.filter(user => {
        const matchesSearch = user.email.toLowerCase().includes(searchQuery.toLowerCase()) ||
            user.fullName.toLowerCase().includes(searchQuery.toLowerCase())

        const matchesOrg = selectedOrg === "all" || user.organizations.some(o => o.id === selectedOrg)
        const matchesRole = selectedRole === "all" || user.roles.includes(selectedRole)
        const matchesStatus = selectedStatus === "all" ||
            (selectedStatus === "locked" && user.isLockedOut) ||
            (selectedStatus === "active" && !user.isLockedOut)

        return matchesSearch && matchesOrg && matchesRole && matchesStatus
    })

    return (
        <div className="space-y-6">
            <div className="flex justify-between items-center">
                <div>
                    <h1 className="text-3xl font-bold tracking-tight text-red-500">System Accounts</h1>
                    <p className="text-muted-foreground">Manage all user accounts across organizations.</p>
                </div>
                <Button onClick={() => setIsCreateDialogOpen(true)} className="bg-red-600 hover:bg-red-700 text-white font-bold">
                    <Plus className="mr-2 h-4 w-4" /> Create User
                </Button>
            </div>

            <div className="flex flex-col sm:flex-row gap-4 items-end">
                <div className="flex-1 w-full space-y-2">
                    <label className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Search</label>
                    <Input
                        placeholder="Search users..."
                        value={searchQuery}
                        onChange={(e) => setSearchQuery(e.target.value)}
                        className="bg-background/50 border-red-900/50 focus-visible:ring-red-500"
                    />
                </div>

                <div className="w-full sm:w-[200px] space-y-2">
                    <label className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Organization</label>
                    <Select value={selectedOrg} onValueChange={setSelectedOrg}>
                        <SelectTrigger className="bg-background/50 border-red-900/50">
                            <SelectValue placeholder="All Organizations" />
                        </SelectTrigger>
                        <SelectContent className="border-red-900/50">
                            <SelectItem value="all">All Organizations</SelectItem>
                            {organizations.map(org => (
                                <SelectItem key={org.id} value={org.id}>{org.name}</SelectItem>
                            ))}
                        </SelectContent>
                    </Select>
                </div>

                <div className="w-full sm:w-[150px] space-y-2">
                    <label className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Role</label>
                    <Select value={selectedRole} onValueChange={setSelectedRole}>
                        <SelectTrigger className="bg-background/50 border-red-900/50">
                            <SelectValue placeholder="All Roles" />
                        </SelectTrigger>
                        <SelectContent className="border-red-900/50">
                            <SelectItem value="all">All Roles</SelectItem>
                            <SelectItem value="SuperAdmin">SuperAdmin</SelectItem>
                            <SelectItem value="Member">Member</SelectItem>
                        </SelectContent>
                    </Select>
                </div>

                <div className="w-full sm:w-[150px] space-y-2">
                    <label className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Status</label>
                    <Select value={selectedStatus} onValueChange={setSelectedStatus}>
                        <SelectTrigger className="bg-background/50 border-red-900/50">
                            <SelectValue placeholder="All Status" />
                        </SelectTrigger>
                        <SelectContent className="border-red-900/50">
                            <SelectItem value="all">All Status</SelectItem>
                            <SelectItem value="active">Active Only</SelectItem>
                            <SelectItem value="locked">Locked Only</SelectItem>
                        </SelectContent>
                    </Select>
                </div>
            </div>

            <div className="rounded-md border border-red-900/50">
                <Table>
                    <TableHeader className="bg-red-950/20">
                        <TableRow className="border-red-900/50">
                            <TableHead className="text-red-400">User</TableHead>
                            <TableHead className="text-red-400">Roles</TableHead>
                            <TableHead className="text-red-400">Status</TableHead>
                            <TableHead className="text-red-400 text-center">Orgs</TableHead>
                            <TableHead className="text-red-400">Created</TableHead>
                            <TableHead className="text-right text-red-400">Actions</TableHead>
                        </TableRow>
                    </TableHeader>
                    <TableBody>
                        {isLoading ? (
                            <TableRow>
                                <TableCell colSpan={6} className="h-24 text-center text-muted-foreground">
                                    Loading accounts...
                                </TableCell>
                            </TableRow>
                        ) : filteredUsers.length === 0 ? (
                            <TableRow>
                                <TableCell colSpan={6} className="h-24 text-center text-muted-foreground">
                                    No accounts found.
                                </TableCell>
                            </TableRow>
                        ) : (
                            filteredUsers.map((user) => (
                                <TableRow key={user.id} className="border-red-900/20 hover:bg-red-950/10">
                                    <TableCell>
                                        <div className="flex flex-col">
                                            <span className="font-medium text-foreground">{user.fullName}</span>
                                            <span className="text-xs text-muted-foreground">{user.email}</span>
                                        </div>
                                    </TableCell>
                                    <TableCell>
                                        <div className="flex gap-1">
                                            {user.roles.map(role => (
                                                <Badge key={role} variant="outline" className={role === 'SuperAdmin' ? 'border-red-500/50 text-red-500' : ''}>
                                                    {role}
                                                </Badge>
                                            ))}
                                        </div>
                                    </TableCell>
                                    <TableCell>
                                        {user.isLockedOut ? (
                                            <Badge variant="destructive" className="bg-red-900/30 border-red-500/50 text-red-500">Locked</Badge>
                                        ) : (
                                            <Badge variant="outline" className="bg-green-500/10 text-green-500 border-green-500/20">Active</Badge>
                                        )}
                                    </TableCell>
                                    <TableCell>
                                        <div className="flex flex-wrap gap-1 justify-center max-w-[150px]">
                                            {user.organizations.map(org => (
                                                <Badge key={org.id} variant="secondary" className="text-[10px] px-1 py-0 h-4 bg-muted/50 border-red-900/30">
                                                    {org.name}
                                                </Badge>
                                            ))}
                                            {user.organizations.length === 0 && (
                                                <span className="text-xs text-muted-foreground italic">None</span>
                                            )}
                                        </div>
                                    </TableCell>
                                    <TableCell className="text-muted-foreground text-xs">
                                        {formatTimeAgo(user.createdAt)}
                                    </TableCell>
                                    <TableCell className="text-right">
                                        <DropdownMenu>
                                            <DropdownMenuTrigger asChild>
                                                <Button variant="ghost" className="h-8 w-8 p-0 hover:bg-red-500/20 hover:text-red-500">
                                                    <span className="sr-only">Open menu</span>
                                                    <MoreHorizontal className="h-4 w-4" />
                                                </Button>
                                            </DropdownMenuTrigger>
                                            <DropdownMenuContent align="end" className="border-red-900/50 min-w-[180px]">
                                                <DropdownMenuItem onClick={() => {
                                                    setUserToManage(user)
                                                    setIsMembershipDialogOpen(true)
                                                }} className="text-red-400 focus:text-red-400 focus:bg-red-500/10">
                                                    <Building className="mr-2 h-4 w-4" /> Manage Organizations
                                                </DropdownMenuItem>
                                                <DropdownMenuItem onClick={() => handleImpersonate(user)} className="text-orange-400 focus:text-orange-400 focus:bg-orange-500/10">
                                                    <ShieldCheck className="mr-2 h-4 w-4" /> Impersonate
                                                </DropdownMenuItem>
                                                <DropdownMenuItem onClick={() => handleToggleLock(user)}>
                                                    {user.isLockedOut ? (
                                                        <><Unlock className="mr-2 h-4 w-4" /> Unlock Access</>
                                                    ) : (
                                                        <><Lock className="mr-2 h-4 w-4 text-red-500" /> Lock Access</>
                                                    )}
                                                </DropdownMenuItem>
                                                <DropdownMenuSeparator className="bg-red-900/30" />
                                                <DropdownMenuItem
                                                    onClick={() => {
                                                        setUserToDelete(user)
                                                        setIsDeleteDialogOpen(true)
                                                    }}
                                                    className="text-red-500 focus:text-red-500 focus:bg-red-500/10"
                                                >
                                                    <UserMinus className="mr-2 h-4 w-4" /> Delete User
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

            <AlertDialog open={isCreateDialogOpen} onOpenChange={setIsCreateDialogOpen}>
                <AlertDialogContent className="border-red-900/50 bg-background sm:max-w-[425px]">
                    <AlertDialogHeader>
                        <AlertDialogTitle className="text-red-500 text-xl font-bold">Create User Account</AlertDialogTitle>
                        <AlertDialogDescription className="text-muted-foreground">
                            Manually bootstrap a new system user.
                        </AlertDialogDescription>
                    </AlertDialogHeader>
                    <div className="space-y-4 py-4">
                        <div className="space-y-2">
                            <label className="text-sm font-medium">Full Name</label>
                            <Input
                                placeholder="John Doe"
                                value={newUser.fullName}
                                onChange={e => setNewUser({ ...newUser, fullName: e.target.value })}
                                className="bg-background/50 border-red-900/50"
                            />
                        </div>
                        <div className="space-y-2">
                            <label className="text-sm font-medium">Email Address</label>
                            <Input
                                type="email"
                                placeholder="john@example.com"
                                value={newUser.email}
                                onChange={e => setNewUser({ ...newUser, email: e.target.value })}
                                className="bg-background/50 border-red-900/50"
                            />
                        </div>
                        <div className="space-y-2">
                            <label className="text-sm font-medium">Temporary Password</label>
                            <Input
                                type="password"
                                value={newUser.password}
                                onChange={e => setNewUser({ ...newUser, password: e.target.value })}
                                className="bg-background/50 border-red-900/50"
                            />
                        </div>
                    </div>
                    <AlertDialogFooter>
                        <AlertDialogCancel className="border-red-900/50 hover:bg-red-950/20">Cancel</AlertDialogCancel>
                        <AlertDialogAction
                            onClick={e => { e.preventDefault(); handleCreateUser(); }}
                            disabled={isCreating}
                            className="bg-red-600 hover:bg-red-700 text-white font-bold"
                        >
                            {isCreating ? "Creating..." : "Create Account"}
                        </AlertDialogAction>
                    </AlertDialogFooter>
                </AlertDialogContent>
            </AlertDialog>

            {/* Manage Membership Dialog */}
            <AlertDialog open={isMembershipDialogOpen} onOpenChange={setIsMembershipDialogOpen}>
                <AlertDialogContent className="border-red-900/50 bg-background sm:max-w-[500px]">
                    <AlertDialogHeader>
                        <AlertDialogTitle className="text-red-500 text-xl font-bold">Manage Organizations</AlertDialogTitle>
                        <AlertDialogDescription className="text-muted-foreground">
                            Add or remove <strong>{userToManage?.fullName}</strong> from organizations.
                        </AlertDialogDescription>
                    </AlertDialogHeader>

                    <div className="space-y-6 py-4">
                        {/* Current Memberships */}
                        <div className="space-y-3">
                            <h3 className="text-sm font-semibold uppercase tracking-wider text-red-400/80">Active Memberships</h3>
                            <div className="space-y-2 max-h-[200px] overflow-y-auto pr-2 custom-scrollbar">
                                {userToManage?.organizations.map(org => (
                                    <div key={org.id} className="flex items-center justify-between p-2 rounded-md bg-red-950/20 border border-red-900/30 group">
                                        <div className="flex items-center space-x-2">
                                            <Building className="h-4 w-4 text-red-500/70" />
                                            <span className="text-sm font-medium">{org.name}</span>
                                        </div>
                                        <Button
                                            variant="ghost"
                                            size="sm"
                                            onClick={() => handleRemoveMembership(org.id)}
                                            className="h-8 px-2 text-muted-foreground hover:text-red-500 hover:bg-red-500/10"
                                            disabled={isUpdatingMembership}
                                        >
                                            <X className="h-4 w-4" />
                                        </Button>
                                    </div>
                                ))}
                                {userToManage?.organizations.length === 0 && (
                                    <div className="text-center py-4 text-xs text-muted-foreground italic border border-dashed border-red-900/30 rounded-md">
                                        No active memberships found.
                                    </div>
                                )}
                            </div>
                        </div>

                        {/* Add New Membership */}
                        <div className="space-y-3 border-t border-red-900/20 pt-4">
                            <h3 className="text-sm font-semibold uppercase tracking-wider text-red-400/80">Add to Organization</h3>
                            <div className="flex gap-2">
                                <div className="flex-1">
                                    <Select value={orgToJoin} onValueChange={setOrgToJoin}>
                                        <SelectTrigger className="bg-background/50 border-red-900/50">
                                            <SelectValue placeholder="Select organization..." />
                                        </SelectTrigger>
                                        <SelectContent className="border-red-900/50">
                                            {organizations
                                                .filter(o => !userToManage?.organizations.some(mo => mo.id === o.id))
                                                .map(org => (
                                                    <SelectItem key={org.id} value={org.id}>{org.name}</SelectItem>
                                                ))
                                            }
                                            {organizations.length === 0 && (
                                                <SelectItem value="none" disabled>No organizations found</SelectItem>
                                            )}
                                        </SelectContent>
                                    </Select>
                                </div>
                                <div className="w-[120px]">
                                    <Select value={roleToJoin} onValueChange={setRoleToJoin}>
                                        <SelectTrigger className="bg-background/50 border-red-900/50">
                                            <SelectValue placeholder="Role" />
                                        </SelectTrigger>
                                        <SelectContent className="border-red-900/50">
                                            <SelectItem value="owner">Owner</SelectItem>
                                            <SelectItem value="Member">Member</SelectItem>
                                        </SelectContent>
                                    </Select>
                                </div>
                                <Button
                                    onClick={handleAddMembership}
                                    disabled={!orgToJoin || isUpdatingMembership}
                                    className="bg-red-600 hover:bg-red-700 text-white"
                                >
                                    Add
                                </Button>
                            </div>
                        </div>
                    </div>

                    <AlertDialogFooter>
                        <AlertDialogCancel className="border-red-900/50 hover:bg-red-950/20">Close</AlertDialogCancel>
                    </AlertDialogFooter>
                </AlertDialogContent>
            </AlertDialog>

            {/* Delete Account Dialog */}
            <AlertDialog open={isDeleteDialogOpen} onOpenChange={setIsDeleteDialogOpen}>
                <AlertDialogContent className="border-red-900/50 bg-background sm:max-w-[425px]">
                    <AlertDialogHeader>
                        <AlertDialogTitle className="text-red-500 text-xl font-bold flex items-center">
                            <UserMinus className="mr-2 h-5 w-5" /> Delete User Account
                        </AlertDialogTitle>
                        <AlertDialogDescription className="text-muted-foreground pt-2">
                            Are you absolutely sure you want to delete <strong>{userToDelete?.fullName}</strong> ({userToDelete?.email})?
                            <br /><br />
                            This action is <strong>permanent</strong> and will remove their identity record and all organization memberships.
                        </AlertDialogDescription>
                    </AlertDialogHeader>
                    <AlertDialogFooter className="mt-4">
                        <AlertDialogCancel>Cancel</AlertDialogCancel>
                        <AlertDialogAction
                            onClick={handleDeleteUser}
                            disabled={isDeleting}
                            className="bg-red-600 hover:bg-red-700 text-white font-bold"
                        >
                            {isDeleting ? "Deleting..." : "Permanently Delete"}
                        </AlertDialogAction>
                    </AlertDialogFooter>
                </AlertDialogContent>
            </AlertDialog>
        </div>
    )
}
