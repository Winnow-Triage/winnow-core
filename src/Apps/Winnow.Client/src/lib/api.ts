import axios from 'axios';

export const api = axios.create({
    baseURL: import.meta.env.VITE_API_URL || 'http://localhost:5294',
});

// Add authentication interceptor
api.interceptors.request.use((config) => {
    const token = localStorage.getItem('authToken');
    if (token) {
        config.headers['Authorization'] = `Bearer ${token}`;
    }

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
            // Token expired or invalid
            localStorage.removeItem('authToken');
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
                localStorage.removeItem('authToken');
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

export const getOrganizationDetails = async (id: string) => {
    const response = await api.get(`/admin/organizations/${id}`);
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
    await api.delete(`/admin/organizations/${orgId}/members/${userId}`);
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
