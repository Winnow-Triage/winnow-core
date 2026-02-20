import { SidebarProvider, SidebarTrigger, SidebarInset } from "@/components/ui/sidebar"
import { AdminSidebar } from "./AdminSidebar"
import { ModeToggle } from "@/components/mode-toggle"
import UserNav from "@/components/UserNav"
import { Outlet } from "react-router-dom"

export default function AdminLayout() {
    return (
        <SidebarProvider>
            <AdminSidebar />
            <SidebarInset>
                <header className="flex h-16 shrink-0 items-center gap-2 border-b border-red-900/50 px-4 bg-red-950/10">
                    <SidebarTrigger className="-ml-1 text-red-500" />
                    <div className="w-[1px] h-4 bg-red-900/50 mx-2" />
                    <div className="flex items-center gap-2">
                        <div className="h-2 w-2 rounded-full bg-red-500 animate-pulse" />
                        <span className="font-medium text-red-500 tracking-wide uppercase text-sm">Super Admin Portal</span>
                    </div>
                    <div className="ml-auto flex items-center gap-2">
                        <ModeToggle />
                        <UserNav />
                    </div>
                </header>
                <div className="flex flex-1 flex-col gap-4 p-4 bg-background/95">
                    <Outlet />
                </div>
            </SidebarInset>
        </SidebarProvider>
    )
}
