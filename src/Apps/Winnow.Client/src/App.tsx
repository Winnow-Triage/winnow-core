import { lazy, Suspense } from "react";
import {
  SidebarProvider,
  SidebarTrigger,
  SidebarInset,
} from "@/components/ui/sidebar";
import { AppSidebar } from "@/components/app-sidebar";
import { Route, Routes, Navigate } from "react-router-dom";
import { ThemeProvider } from "@/components/theme-provider";
import Layout from "./components/Layout";

// Core Pages
const ClusterDashboard = lazy(() => import("@/pages/ClusterDashboard"));
const ReviewSuggestions = lazy(() => import("@/pages/ReviewSuggestions"));
const ReportDetail = lazy(() => import("@/pages/ReportDetail"));
const DebugConsole = lazy(() => import("./pages/DebugConsole"));
const AllReports = lazy(() => import("./pages/AllReports"));
const Clusters = lazy(() => import("./pages/Clusters"));
const Settings = lazy(() => import("./pages/Settings"));
const ProjectSettings = lazy(() => import("./pages/ProjectSettings"));
const ProjectSetup = lazy(() => import("./pages/ProjectSetup"));
const ClusterDetail = lazy(() => import("./pages/ClusterDetail"));
const OrganizationDashboard = lazy(() => import("./pages/OrganizationDashboard"));
const TeamDashboard = lazy(() => import("./pages/TeamDashboard"));
const AuthPage = lazy(() => import("./pages/AuthPage"));
const SuspendedPage = lazy(() => import("./pages/SuspendedPage"));
const UserSettings = lazy(() => import("./pages/UserSettings"));
const AcceptInvitationPage = lazy(() => import("./pages/AcceptInvitationPage"));
const ForgotPasswordPage = lazy(() => import("./pages/ForgotPasswordPage"));
const ResetPasswordPage = lazy(() => import("./pages/ResetPasswordPage"));
const VerifyEmailPage = lazy(() => import("./pages/VerifyEmailPage"));

// Admin Pages
import AdminLayout from "./components/admin/AdminLayout";
import AdminProtectedRoute from "./components/admin/AdminProtectedRoute";
const OrganizationsDashboard = lazy(() => import("./pages/admin/OrganizationsDashboard"));
const UsersDashboard = lazy(() => import("./pages/admin/UsersDashboard"));
const SystemHealth = lazy(() => import("./pages/admin/SystemHealth"));
const AdminSettings = lazy(() => import("./pages/admin/AdminSettings"));
const TicketsDashboard = lazy(() => import("./pages/admin/TicketsDashboard"));

import { Toaster } from "sonner";

import { ModeToggle } from "@/components/mode-toggle";
import { AboutDialog } from "@/components/AboutDialog";
import ProtectedRoute from "@/components/ProtectedRoute";
import UserNav from "@/components/UserNav";
import VerificationBanner from "./components/VerificationBanner";
import { Loader2 } from "lucide-react";
import { AuthProvider } from "./context/AuthContext";

import { WinnowLogo } from "./components/WinnowLogo";
import PermissionProtectedRoute from "./components/PermissionProtectedRoute";

export default function App() {
  return (
    <ThemeProvider defaultTheme="dark" storageKey="vite-ui-theme">
      <AuthProvider>
        <Suspense
          fallback={
            <div className="flex h-screen w-full items-center justify-center bg-background">
              <Loader2 className="h-8 w-8 animate-spin text-primary" />
            </div>
          }
        >
          <Routes>
            <Route path="/login" element={<AuthPage />} />
            <Route path="/signup" element={<AuthPage />} />
            <Route path="/suspended" element={<SuspendedPage />} />
            <Route path="/accept-invite" element={<AcceptInvitationPage />} />
            <Route path="/forgot-password" element={<ForgotPasswordPage />} />
            <Route path="/reset-password" element={<ResetPasswordPage />} />
            <Route path="/verify-email" element={<VerifyEmailPage />} />

            <Route
              path="/admin/*"
              element={
                <AdminProtectedRoute>
                  <Routes>
                    <Route element={<AdminLayout />}>
                      <Route
                        index
                        element={<Navigate to="/admin/organizations" replace />}
                      />
                      <Route
                        path="organizations"
                        element={<OrganizationsDashboard />}
                      />
                      <Route path="users" element={<UsersDashboard />} />
                      <Route path="tickets" element={<TicketsDashboard />} />
                      <Route path="health" element={<SystemHealth />} />
                      <Route path="settings" element={<AdminSettings />} />
                    </Route>
                  </Routes>
                </AdminProtectedRoute>
              }
            />

            <Route
              path="/*"
              element={
                <ProtectedRoute>
                  <VerificationBanner />
                  <SidebarProvider>
                    <AppSidebar />
                    <SidebarInset>
                      <header className="flex h-16 shrink-0 items-center gap-2 border-b px-4">
                        <SidebarTrigger className="-ml-1" />
                        <div className="w-[1px] h-4 bg-border mx-2" />
                        <WinnowLogo size={24} />
                        <div className="ml-auto flex items-center gap-2">
                          <AboutDialog />
                          <ModeToggle />
                          <UserNav />
                        </div>
                      </header>
                      <div className="flex flex-1 flex-col gap-4 p-4">
                        <Routes>
                          <Route path="/" element={<Layout />}>
                            <Route
                              index
                              element={<Navigate to="/dashboard" replace />}
                            />
                            <Route
                              path="/dashboard"
                              element={<ClusterDashboard />}
                            />
                            <Route
                              path="/org-dashboard"
                              element={<OrganizationDashboard />}
                            />
                            <Route
                              path="/team-dashboard"
                              element={<TeamDashboard />}
                            />
                            <Route
                              path="/triage/review"
                              element={<ReviewSuggestions />}
                            />
                            <Route path="reports" element={<AllReports />} />
                            <Route
                              path="reports/:id"
                              element={<ReportDetail />}
                            />
                            <Route path="clusters" element={<Clusters />} />
                            <Route
                              path="clusters/:id"
                              element={<ClusterDetail />}
                            />
                            <Route path="debug" element={<DebugConsole />} />
                            <Route path="settings" element={<Settings />} />
                            <Route
                              path="settings/user"
                              element={<UserSettings />}
                            />
                            <Route
                              path="project-settings"
                              element={
                                <PermissionProtectedRoute permission="projects:manage">
                                  <ProjectSettings />
                                </PermissionProtectedRoute>
                              }
                            />
                            <Route path="setup" element={<ProjectSetup />} />
                          </Route>
                        </Routes>
                      </div>
                    </SidebarInset>
                  </SidebarProvider>
                </ProtectedRoute>
              }
            />
          </Routes>
        </Suspense>
        <Toaster />
      </AuthProvider>
    </ThemeProvider>
  );
}
