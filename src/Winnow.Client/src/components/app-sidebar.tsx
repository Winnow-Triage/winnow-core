import { Ticket, LayoutDashboard, Settings, Inbox, Layers } from "lucide-react"
import { Link } from "react-router-dom"
import {
    Sidebar,
    SidebarContent,
    SidebarFooter,
    SidebarGroup,
    SidebarGroupContent,
    SidebarGroupLabel,
    SidebarMenu,
    SidebarMenuButton,
    SidebarMenuItem,
    SidebarRail,
    SidebarTrigger,
} from "@/components/ui/sidebar"
import { ModeToggle } from "@/components/mode-toggle"

// Menu items.
const items = [
    {
        title: "Cluster Dashboard",
        url: "/",
        icon: LayoutDashboard,
    },
    {
        title: "All Tickets",
        url: "/tickets",
        icon: Ticket,
    },
    {
        title: "Clusters",
        url: "/clusters",
        icon: Layers,
    },
    {
        title: "Debug Console",
        url: "/debug",
        icon: Inbox,
    },
    {
        title: "Settings",
        url: "/settings",
        icon: Settings,
    },
]

export function AppSidebar() {
    return (
        <Sidebar collapsible="icon" variant="sidebar">
            <SidebarContent>
                <SidebarGroup>
                    <SidebarGroupLabel>Winnow Triage</SidebarGroupLabel>
                    <SidebarGroupContent>
                        <SidebarMenu>
                            {items.map((item) => (
                                <SidebarMenuItem key={item.title}>
                                    <SidebarMenuButton asChild>
                                        <Link to={item.url}>
                                            <item.icon />
                                            <span>{item.title}</span>
                                        </Link>
                                    </SidebarMenuButton>
                                </SidebarMenuItem>
                            ))}
                        </SidebarMenu>
                    </SidebarGroupContent>
                </SidebarGroup>
            </SidebarContent>
            <SidebarFooter>
                <div className="flex w-full items-center justify-between p-2 gap-2">
                    <SidebarTrigger className="h-4 w-4 text-muted-foreground hover:text-foreground" />
                    <ModeToggle />
                </div>
            </SidebarFooter>
            <SidebarRail />
        </Sidebar>
    )
}