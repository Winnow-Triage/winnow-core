import axios from 'axios';

export const api = axios.create({
    baseURL: import.meta.env.VITE_API_URL || 'http://localhost:5294',
    withCredentials: true,
});

// Add authentication interceptor
// Add authentication interceptor
api.interceptors.request.use((config) => {
    // Add project ID to all requests
    const projectId = localStorage.getItem('lastProjectId');
    if (projectId) {
        config.headers['X-Project-ID'] = projectId;
    }

    return config;
});

// Add response interceptor to handle token expiration
api.interceptors.response.use(
    (response) => response,
    (error) => {
        if (error.response?.status === 401) {
            // Session expired or invalid
            localStorage.removeItem('user');
            localStorage.removeItem('lastProjectId');

            // Use window.location for navigation since we're outside React component
            if (window.location.pathname !== '/login' && window.location.pathname !== '/signup') {
                window.location.href = '/login';
            }
        }

        // Handle 403 Forbidden for Suspended Organizations
        if (error.response?.status === 403) {
            const errorMessage = error.response?.data?.message?.toLowerCase() || '';
            if (errorMessage.includes('suspended')) {
                // Clear session data to forcefully block further authenticated attempts under this tenant
                localStorage.removeItem('user');
                localStorage.removeItem('lastProjectId');

                if (window.location.pathname !== '/suspended') {
                    window.location.href = '/suspended';
                }
            }
        }
        return Promise.reject(error);
    }
);

// --- Admin Endpoints ---

export interface OrganizationSummary {
    id: string;
    name: string;
    stripeCustomerId: string | null;
    subscriptionTier: string;
    createdAt: string;
    isSuspended: boolean;
    teamCount: number;
    memberCount: number;
    projectCount: number;
}

export const getAllOrganizations = async (): Promise<OrganizationSummary[]> => {
    const response = await api.get('/admin/organizations');
    return response.data;
};

export const updateOrganizationStatus = async (id: string, isSuspended: boolean) => {
    const response = await api.patch(`/admin/organizations/${id}/status`, {
        id,
        isSuspended
    });
    return response.data;
};

export const deleteOrganization = async (id: string) => {
    const response = await api.delete(`/admin/organizations/${id}`);
    return response.data;
};

export const updateOrganizationSubscription = async (id: string, tier: string) => {
    const response = await api.post(`/admin/organizations/${id}/subscription`, {
        subscriptionTier: tier
    });
    return response.data;
};

export interface BillingStatusResponse {
    subscriptionTier: string;
    reportsUsedThisMonth: number;
    reportLimit: number | null;
    hasActiveSubscription: boolean;
}

export const getBillingStatus = async (): Promise<BillingStatusResponse> => {
    const response = await api.get('/billing/status');
    return response.data;
};

export const createOrganization = async (name: string, tier: string = "Free") => {
    const response = await api.post('/admin/organizations', {
        name,
        subscriptionTier: tier
    });
    return response.data;
};



export const addOrganizationMember = async (orgId: string, role: string = "owner", userId?: string) => {
    const response = await api.post(`/admin/organizations/${orgId}/members`, {
        organizationId: orgId,
        role,
        userId
    });
    return response.data;
};

export const removeOrganizationMember = async (orgId: string, userId: string) => {
    await api.delete(`/organizations/${orgId}/members/${userId}`);
};

export const adminRemoveOrganizationMember = async (orgId: string, userId: string) => {
    await api.delete(`/admin/organizations/${orgId}/members/${userId}`);
};

export const toggleMemberLock = async (orgId: string, userId: string) => {
    const response = await api.put(`/organizations/${orgId}/members/${userId}/lock`, {});
    return response.data;
};

export const resendInvitation = async (orgId: string, invitationId: string) => {
    await api.post(`/organizations/${orgId}/invitations/${invitationId}/resend`, {});
};

export const cancelInvitation = async (orgId: string, invitationId: string) => {
    await api.delete(`/organizations/${orgId}/invitations/${invitationId}`);
};

// Auth
export const logoutUser = async () => {
    await api.post('/auth/logout');
    localStorage.removeItem('user');
    localStorage.removeItem('lastProjectId');
};

export const getMe = async () => {
    const { data } = await api.get('/auth/me');
    return data;
};

// Account Management
export const getAccountDetails = async () => {
    const { data } = await api.get('/account/me');
    return data;
};

export const updateAccountDetails = async (fullName: string, email: string) => {
    const { data } = await api.put('/account/me', { fullName, email });
    return data;
};

export const changePassword = async (currentPassword: string, newPassword: string) => {
    await api.post('/account/change-password', { currentPassword, newPassword });
};

// Admin User Management
export interface UserSummary {
    id: string;
    email: string;
    fullName: string;
    roles: string[];
    createdAt: string;
    isLockedOut: boolean;
    organizations: { id: string, name: string }[];
}

export const getAllUsers = async (): Promise<UserSummary[]> => {
    const response = await api.get('/admin/users');
    return response.data;
};

export const adminCreateUser = async (data: { email: string, fullName: string, password: string, role: string }) => {
    const response = await api.post('/admin/users', data);
    return response.data;
};

export const toggleUserLock = async (userId: string) => {
    const response = await api.post(`/admin/users/${userId}/toggle-lock`, { id: userId });
    return response.data;
};

export const impersonateUser = async (userId: string) => {
    const response = await api.post(`/admin/users/${userId}/impersonate`, { id: userId });
    return response.data;
};

export const adminDeleteUser = async (userId: string) => {
    await api.delete(`/admin/users/${userId}`);
};

export interface SystemHealthCheck {
    name: string;
    status: string;
    duration: string;
    description?: string;
}

export interface SystemHealthResponse {
    status: string;
    totalDuration: string;
    utcTimestamp: string;
    checks: SystemHealthCheck[];
}

export const getSystemHealth = async (): Promise<SystemHealthResponse> => {
    const response = await api.get('/health/detailed');
    return response.data;
};

// Admin Report Management
export interface QuotaStatus {
    baseLimit: number;
    graceLimit: number;
    monthlyReportCount: number;
    isOverage: boolean;
    isLocked: boolean;
}

export interface ProjectQuotaSummary {
    id: string;
    name: string;
    monthlyReportCount: number;
}

export interface TeamSummary {
    id: string;
    name: string;
    createdAt: string;
    projectCount: number;
}

export interface MemberSummary {
    id: string;
    userId: string;
    role: string;
    joinedAt: string;
    userEmail?: string;
    userFullName?: string;
}

export interface OrganizationDetailsResponse {
    id: string;
    name: string;
    stripeCustomerId: string | null;
    subscriptionTier: string;
    createdAt: string;
    isPaidTier: boolean;
    teamCount: number;
    memberCount: number;
    projectCount: number;
    reportCount: number;
    assetCount: number;
    integrationCount: number;
    lastReportDate: string | null;
    lastMemberJoinDate: string | null;
    teams: TeamSummary[];
    members: MemberSummary[];
    quota: QuotaStatus;
    projectQuotas: ProjectQuotaSummary[];
}

export const getOrganizationDetails = async (id: string): Promise<OrganizationDetailsResponse> => {
    const response = await api.get(`/admin/organizations/${id}`);
    return response.data;
};

// Admin Report Management
export interface AdminReportResponse {
    id: string;
    projectId: string;
    organizationId: string;
    title: string;
    status: string;
    isLocked: boolean;
    isOverage: boolean;
    createdAt: string;
}

export const getAdminReport = async (id: string): Promise<AdminReportResponse> => {
    const response = await api.get(`/admin/reports/${id}`);
    return response.data;
};

export const toggleAdminReportLock = async (id: string) => {
    const response = await api.post(`/admin/reports/${id}/toggle-lock`, {});
    return response.data;
};

export const resetAdminReportOverage = async (id: string) => {
    const response = await api.post(`/admin/reports/${id}/reset-overage`, {});
    return response.data;
};

export interface AdminReportSummary {
    id: string;
    title: string;
    status: string;
    isLocked: boolean;
    isOverage: boolean;
    createdAt: string;
    organizationId: string;
    organizationName: string;
    projectId: string;
    projectName: string;
}

export interface PagedAdminReportResponse {
    items: AdminReportSummary[];
    totalCount: number;
    page: number;
    pageSize: number;
    totalPages: number;
}

export const getAllAdminReports = async (params?: {
    searchTerm?: string;
    status?: string;
    isLocked?: boolean;
    organizationId?: string;
    projectId?: string;
    page?: number;
    pageSize?: number;
}): Promise<PagedAdminReportResponse> => {
    const response = await api.get('/admin/reports', { params });
    return response.data;
};

// Organization and Team Dashboards
export interface DashboardQuotaStatus {
    totalUsage: number;
    baseLimit: number | null;
    graceLimit: number | null;
    isOverage: boolean;
    usageHistory: { month: string; reportCount: number; clusterCount: number }[];
}

export interface TeamBreakdown {
    teamId: string;
    teamName: string;
    projectCount: number;
    reportVolume: number;
}

export interface TopProject {
    projectId: string;
    projectName: string;
    reportCount: number;
    activeClusters: number;
}

export interface OrganizationDashboardMetrics {
    quota: DashboardQuotaStatus;
    teamBreakdown: TeamBreakdown[];
    topProjects: TopProject[];
}

export const getOrganizationMetrics = async (): Promise<OrganizationDashboardMetrics> => {
    const response = await api.get('/dashboard/organization/metrics');
    return response.data;
};

export interface ProjectBreakdown {
    projectId: string;
    projectName: string;
    reportVolume: number;
    activeClusters: number;
}

export interface TeamDashboardMetrics {
    projectBreakdown: ProjectBreakdown[];
    topClusters: {
        clusterId: string;
        title: string;
        status: string;
        reportCount: number;
        velocity: number;
        isHot: boolean;
    }[];
    volumeHistory: {
        timestamp: string;
        newUniqueCount: number;
        duplicateCount: number;
    }[];
}

export const getTeamMetrics = async (teamId: string): Promise<TeamDashboardMetrics> => {
    const response = await api.get(`/dashboard/teams/${teamId}/metrics`);
    return response.data;
};

export const getMyTeams = async () => {
    const response = await api.get('/teams');
    return response.data;
};
