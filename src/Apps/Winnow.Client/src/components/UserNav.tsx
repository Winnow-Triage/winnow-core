import { useNavigate } from "react-router-dom"
import {
    DropdownMenu,
    DropdownMenuContent,
    DropdownMenuItem,
    DropdownMenuLabel,
    DropdownMenuSeparator,
    DropdownMenuTrigger,
    DropdownMenuSub,
    DropdownMenuSubTrigger,
    DropdownMenuSubContent,
    DropdownMenuPortal,
} from "@/components/ui/dropdown-menu"
import { Button } from "@/components/ui/button"
import { Avatar, AvatarFallback } from "@/components/ui/avatar"
import { useQuery } from "@tanstack/react-query"
import { api, logoutUser } from "@/lib/api"
import { toast } from "sonner"
import { useState } from "react"
import { Check, Building2 } from "lucide-react"

export default function UserNav() {
    const navigate = useNavigate()

    // Retrieve user from localStorage
    const userString = localStorage.getItem("user")
    const user = userString ? JSON.parse(userString) : { name: "User", email: "user@example.com", organizationId: "" }

    // Get initials
    const initials = user.name
        .split(" ")
        .map((n: string) => n[0])
        .join("")
        .toUpperCase()
        .substring(0, 2);

    const { data: orgs } = useQuery<{ id: string, name: string }[]>({
        queryKey: ["user-organizations"],
        queryFn: async () => {
            const { data } = await api.get("/organizations");
            return data;
        }
    });

    const [isSwitching, setIsSwitching] = useState<string | null>(null);

    const handleSwitch = async (orgId: string) => {
        if (isSwitching || orgId === user.organizationId) return;
        setIsSwitching(orgId);
        try {
            const { data } = await api.post("/auth/switch", { organizationId: orgId });
            // Fully discard stale data
            localStorage.removeItem("lastProjectId");

            localStorage.setItem("user", JSON.stringify({
                id: data.userId,
                email: data.email,
                name: data.fullName,
                defaultProjectId: data.defaultProjectId,
                organizationId: data.activeOrganizationId
            }));
            toast.success(`Switched to ${orgs?.find(o => o.id === orgId)?.name}`);
            setTimeout(() => {
                window.location.href = "/dashboard";
            }, 500);
        } catch (error) {
            console.error("Failed to switch organization:", error);
            toast.error("Failed to switch organization");
        } finally {
            setIsSwitching(null);
        }
    };

    const handleLogout = async () => {
        try {
            await logoutUser();
            navigate("/login");
        } catch (error) {
            console.error("Logout failed:", error);
            // Fallback: clear local storage and redirect anyway
            localStorage.removeItem("user");
            localStorage.removeItem("lastProjectId");
            navigate("/login");
        }
    }

    return (
        <DropdownMenu>
            <DropdownMenuTrigger asChild>
                <Button variant="ghost" className="relative h-8 w-8 rounded-full p-0">
                    <Avatar className="h-8 w-8">
                        <AvatarFallback>{initials}</AvatarFallback>
                    </Avatar>
                </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent className="w-56" align="end" forceMount>
                <DropdownMenuLabel className="font-normal">
                    <div className="flex flex-col space-y-1">
                        <p className="text-sm font-medium leading-none">{user.name}</p>
                        <p className="text-xs leading-none text-muted-foreground">
                            {user.email}
                        </p>
                    </div>
                </DropdownMenuLabel>
                <DropdownMenuSeparator />
                <DropdownMenuItem onClick={() => navigate("/settings/user")}>
                    User Settings
                </DropdownMenuItem>
                <DropdownMenuItem onClick={() => navigate("/settings")}>
                    Workspace Settings
                </DropdownMenuItem>

                {orgs && orgs.length > 1 && (
                    <DropdownMenuSub>
                        <DropdownMenuSubTrigger>
                            <Building2 className="mr-2 h-4 w-4" />
                            <span>Switch Workspace</span>
                        </DropdownMenuSubTrigger>
                        <DropdownMenuPortal>
                            <DropdownMenuSubContent className="w-48">
                                {orgs.map((org) => {
                                    const isCurrent = org.id === user.organizationId;
                                    return (
                                        <DropdownMenuItem
                                            key={org.id}
                                            onClick={() => handleSwitch(org.id)}
                                            disabled={!!isSwitching}
                                            className="flex items-center"
                                        >
                                            <span className="truncate">{org.name}</span>
                                            {isCurrent && <Check className="ml-auto h-4 w-4" />}
                                        </DropdownMenuItem>
                                    );
                                })}
                            </DropdownMenuSubContent>
                        </DropdownMenuPortal>
                    </DropdownMenuSub>
                )}

                <DropdownMenuSeparator />
                <DropdownMenuItem onClick={handleLogout} className="text-red-500 focus:text-red-500">
                    Log out
                </DropdownMenuItem>
            </DropdownMenuContent>
        </DropdownMenu>
    )
}
