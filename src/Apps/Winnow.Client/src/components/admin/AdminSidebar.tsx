import { Building2, Users, ShieldAlert, Settings, Ticket } from "lucide-react";
import { Link, useLocation } from "react-router-dom";
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
} from "@/components/ui/sidebar";
import { WinnowLogo } from "../WinnowLogo";

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
    title: "Tickets",
    url: "/admin/tickets",
    icon: Ticket,
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
];
export function AdminSidebar() {
  const location = useLocation();

  return (
    <Sidebar
      collapsible="icon"
      variant="sidebar"
      className="border-r-red-900/50"
    >
      <SidebarContent>
        <div className="p-4 flex items-center gap-2">
          <WinnowLogo size={28} showText={true} />
        </div>
        <SidebarGroup>
          <SidebarGroupLabel className="text-red-500/70">
            Administration
          </SidebarGroupLabel>
          <SidebarGroupContent>
            <SidebarMenu>
              {items.map((item) => {
                const isActive = location.pathname.startsWith(item.url);
                return (
                  <SidebarMenuItem key={item.title}>
                    <SidebarMenuButton
                      asChild
                      tooltip={item.title}
                      isActive={isActive}
                    >
                      <Link
                        to={item.url}
                        className={`hover:bg-red-500/10 hover:text-red-400 ${isActive ? "bg-red-500/20 text-red-500 font-semibold" : ""}`}
                      >
                        <item.icon className="text-red-500/70" />
                        <span>{item.title}</span>
                      </Link>
                    </SidebarMenuButton>
                  </SidebarMenuItem>
                );
              })}
            </SidebarMenu>
          </SidebarGroupContent>
        </SidebarGroup>
      </SidebarContent>
      <SidebarRail />
    </Sidebar>
  );
}
