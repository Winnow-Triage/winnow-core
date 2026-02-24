import { Building2, Users, ShieldAlert, Settings } from "lucide-react"
import { Link } from "react-router-dom"
import {
    Sidebar,
    SidebarContent,
    SidebarGroup,
    SidebarGroupContent,
    SidebarGroupLabel,
    SidebarMenu,
    SidebarMenuButton,
    SidebarMenuItem,
    SidebarRail,
} from "@/components/ui/sidebar"

const items = [
    {
        title: "Organizations",
        url: "/admin/organizations",
        icon: Building2,
    },
    {
        title: "Accounts",
        url: "/admin/users",
        icon: Users,
    },
    {
        title: "System Health",
        url: "/admin/health",
        icon: ShieldAlert,
    },
    {
        title: "Admin Settings",
        url: "/admin/settings",
        icon: Settings,
    },
]

export function AdminSidebar() {
    return (
        <Sidebar collapsible="icon" variant="sidebar" className="border-r-red-900/50">
            <SidebarContent>
                <div className="p-4 flex items-center gap-2">
                    <div className="relative flex h-8 w-8 items-center justify-center rounded-lg bg-red-600 text-white shadow-lg shadow-red-900/50">
                        <span className="font-mono text-lg font-bold">A</span>
                    </div>
                    <span className="font-bold text-lg tracking-tight text-red-500">Super Admin</span>
                </div>
                <SidebarGroup>
                    <SidebarGroupLabel className="text-red-500/70">Administration</SidebarGroupLabel>
                    <SidebarGroupContent>
                        <SidebarMenu>
                            {items.map((item) => (
                                <SidebarMenuItem key={item.title}>
                                    <SidebarMenuButton asChild tooltip={item.title}>
                                        <Link to={item.url} className="hover:bg-red-500/10 hover:text-red-400">
                                            <item.icon className="text-red-500/70" />
                                            <span>{item.title}</span>
                                        </Link>
                                    </SidebarMenuButton>
                                </SidebarMenuItem>
                            ))}
                        </SidebarMenu>
                    </SidebarGroupContent>
                </SidebarGroup>
            </SidebarContent>
            <SidebarRail />
        </Sidebar>
    )
}
