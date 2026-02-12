import axios from 'axios';

export const api = axios.create({
    baseURL: import.meta.env.VITE_API_URL || 'http://localhost:5294',
});

// Add tenant interceptor if needed later
api.interceptors.request.use((config) => {
    // For local dev, let's pretend we are Tenant-A
    config.headers['X-Tenant-ID'] = 'Tenant-A';
    return config;
});
