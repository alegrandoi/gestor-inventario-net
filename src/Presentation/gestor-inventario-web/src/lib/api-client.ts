import axios, { AxiosHeaders, type InternalAxiosRequestConfig } from 'axios';

const baseURL = process.env.NEXT_PUBLIC_API_BASE_URL ?? 'http://localhost:5000/api';

export const apiClient = axios.create({
  baseURL: baseURL.endsWith('/api') ? baseURL : `${baseURL}/api`,
  withCredentials: false,
  headers: {
    'Content-Type': 'application/json'
  }
});

if (typeof window !== 'undefined') {
  const persistedTenantId = window.localStorage.getItem('tenantId');
  if (persistedTenantId) {
    apiClient.defaults.headers.common['X-Tenant-Id'] = persistedTenantId;
  }

  apiClient.interceptors.request.use((config: InternalAxiosRequestConfig) => {
    const tenantId = window.localStorage.getItem('tenantId');
    const headers = AxiosHeaders.from(config.headers ?? {});

    if (tenantId) {
      headers.set('X-Tenant-Id', tenantId);
    } else {
      headers.delete('X-Tenant-Id');
    }

    config.headers = headers;

    return config;
  });
}
