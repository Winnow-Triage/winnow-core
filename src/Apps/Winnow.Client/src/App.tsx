import { SidebarProvider, SidebarTrigger, SidebarInset } from "@/components/ui/sidebar"
import { AppSidebar } from "@/components/app-sidebar"
import { Route, Routes, Navigate } from "react-router-dom"
import { ThemeProvider } from "@/components/theme-provider"
import Layout from './components/Layout'
import ClusterDashboard from "@/pages/ClusterDashboard"
import ReviewSuggestions from "@/pages/ReviewSuggestions"
import ReportDetail from "@/pages/ReportDetail"
import DebugConsole from './pages/DebugConsole'
import AllReports from './pages/AllReports'
import Clusters from './pages/Clusters'
import Settings from './pages/Settings'
import ProjectSettings from './pages/ProjectSettings'
import ProjectSetup from './pages/ProjectSetup'
import AuthPage from './pages/AuthPage'
import SuspendedPage from './pages/SuspendedPage'
import UserSettings from './pages/UserSettings'
import AcceptInvitationPage from './pages/AcceptInvitationPage'

import AdminLayout from './components/admin/AdminLayout'
import AdminProtectedRoute from './components/admin/AdminProtectedRoute'
import OrganizationsDashboard from './pages/admin/OrganizationsDashboard'
import UsersDashboard from './pages/admin/UsersDashboard'
import SystemHealth from './pages/admin/SystemHealth'
import AdminSettings from './pages/admin/AdminSettings'

import { Toaster } from "sonner"

import { ModeToggle } from "@/components/mode-toggle"
import { AboutDialog } from "@/components/AboutDialog"
import ProtectedRoute from "@/components/ProtectedRoute"
import UserNav from "@/components/UserNav"

export default function App() {
  return (
    <ThemeProvider defaultTheme="dark" storageKey="vite-ui-theme">
      <Routes>
        <Route path="/login" element={<AuthPage />} />
        <Route path="/signup" element={<AuthPage />} />
        <Route path="/suspended" element={<SuspendedPage />} />
        <Route path="/accept-invite" element={<AcceptInvitationPage />} />

        <Route path="/admin/*" element={
          <AdminProtectedRoute>
            <Routes>
              <Route element={<AdminLayout />}>
                <Route index element={<Navigate to="/admin/organizations" replace />} />
                <Route path="organizations" element={<OrganizationsDashboard />} />
                <Route path="users" element={<UsersDashboard />} />
                <Route path="health" element={<SystemHealth />} />
                <Route path="settings" element={<AdminSettings />} />
              </Route>
            </Routes>
          </AdminProtectedRoute>
        } />

        <Route path="/*" element={
          <ProtectedRoute>
            <SidebarProvider>
              <AppSidebar />
              <SidebarInset>
                <header className="flex h-16 shrink-0 items-center gap-2 border-b px-4">
                  <SidebarTrigger className="-ml-1" />
                  <div className="w-[1px] h-4 bg-border mx-2" />
                  <span className="font-medium">Winnow Triage</span>
                  <div className="ml-auto flex items-center gap-2">
                    <AboutDialog />
                    <ModeToggle />
                    <UserNav />
                  </div>
                </header>
                <div className="flex flex-1 flex-col gap-4 p-4">
                  <Routes>
                    <Route path="/" element={<Layout />}>
                      <Route index element={<Navigate to="/dashboard" replace />} />
                      <Route path="/dashboard" element={<ClusterDashboard />} />
                      <Route path="/triage/review" element={<ReviewSuggestions />} />
                      <Route path="reports" element={<AllReports />} />
                      <Route path="reports/:id" element={<ReportDetail />} />
                      <Route path="clusters" element={<Clusters />} />
                      <Route path="debug" element={<DebugConsole />} />
                      <Route path="settings" element={<Settings />} />
                      <Route path="settings/user" element={<UserSettings />} />
                      <Route path="project-settings" element={<ProjectSettings />} />
                      <Route path="setup" element={<ProjectSetup />} />
                    </Route>
                  </Routes>
                </div>
              </SidebarInset>
            </SidebarProvider>
          </ProtectedRoute>
        } />
      </Routes>
      <Toaster />
    </ThemeProvider>
  )
}