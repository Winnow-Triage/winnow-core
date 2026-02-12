import { SidebarProvider, SidebarTrigger, SidebarInset } from "@/components/ui/sidebar"
import { AppSidebar } from "@/components/app-sidebar"
import { Route, Routes } from "react-router-dom" // Assuming these imports are needed for the new routes
import Layout from './components/Layout'
import ClusterDashboard from './pages/ClusterDashboard'
import TicketDetail from './pages/TicketDetail'
import DebugConsole from './pages/DebugConsole'
import AllTickets from './pages/AllTickets'
import Clusters from './pages/Clusters'

export default function App() {
  return (
    <SidebarProvider>
      <AppSidebar />
      <SidebarInset>
        <header className="flex h-16 shrink-0 items-center gap-2 border-b px-4">
          <SidebarTrigger className="-ml-1" />
          <div className="w-[1px] h-4 bg-border mx-2" />
          <span className="font-medium">Winnow Triage</span>
        </header>
        <div className="flex flex-1 flex-col gap-4 p-4">
          <Routes>
            <Route path="/" element={<Layout />}>
              <Route index element={<ClusterDashboard />} />
              <Route path="tickets" element={<AllTickets />} />
              <Route path="tickets/:id" element={<TicketDetail />} />
              <Route path="clusters" element={<Clusters />} />
              <Route path="debug" element={<DebugConsole />} />
              <Route path="settings" element={<div>Settings (Coming Soon)</div>} />
            </Route>
          </Routes>
        </div>
      </SidebarInset>
    </SidebarProvider>
  )
}