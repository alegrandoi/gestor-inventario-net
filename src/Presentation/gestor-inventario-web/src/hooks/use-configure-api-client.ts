'use client';

import { useEffect, useState } from 'react';
import { apiClient } from '../lib/api-client';
import { useAuthStore } from '../store/auth-store';

interface ApiClientConfigurationState {
  isConfigured: boolean;
  tenantId: string | null;
}

const tenantStorageKey = 'tenantId';
const tenantChangeEventName = 'tenant-id-change';

type TenantIdChangeEvent = CustomEvent<{ tenantId: string | null }>;

let hasPatchedLocalStorage = false;

function dispatchTenantChange(tenantId: string | null) {
  if (typeof window === 'undefined') {
    return;
  }

  window.dispatchEvent(
    new CustomEvent<{ tenantId: string | null }>(tenantChangeEventName, {
      detail: { tenantId }
    })
  );
}

function ensureLocalStorageTenantBroadcast() {
  if (hasPatchedLocalStorage || typeof window === 'undefined') {
    return;
  }

  const storage = window.localStorage;
  const originalSetItem = storage.setItem;
  const originalRemoveItem = storage.removeItem;
  const originalClear = storage.clear;

  storage.setItem = function setItem(key: string, value: string) {
    originalSetItem.call(this, key, value);

    if (key === tenantStorageKey) {
      dispatchTenantChange(value);
    }
  };

  storage.removeItem = function removeItem(key: string) {
    const hadTenant = key === tenantStorageKey ? storage.getItem(tenantStorageKey) !== null : false;
    originalRemoveItem.call(this, key);

    if (key === tenantStorageKey && hadTenant) {
      dispatchTenantChange(null);
    }
  };

  storage.clear = function clear() {
    const hadTenant = storage.getItem(tenantStorageKey) !== null;
    originalClear.call(this);

    if (hadTenant) {
      dispatchTenantChange(null);
    }
  };

  hasPatchedLocalStorage = true;
}

function applyTenantHeader(tenantId: string | null) {
  if (tenantId) {
    apiClient.defaults.headers.common['X-Tenant-Id'] = tenantId;
  } else {
    delete apiClient.defaults.headers.common['X-Tenant-Id'];
  }
}

/**
 * Ensures the shared Axios client is configured with the latest base URL,
 * authorization token and tenant headers before performing API calls.
 * Returns a flag that indicates when the configuration has been applied.
 */
export function useConfigureApiClient(): ApiClientConfigurationState {
  const token = useAuthStore((state) => state.token);
  const [state, setState] = useState<ApiClientConfigurationState>(() => ({
    isConfigured: false,
    tenantId: typeof window === 'undefined' ? null : window.localStorage.getItem(tenantStorageKey)
  }));

  useEffect(() => {
    if (token) {
      apiClient.defaults.headers.common.Authorization = `Bearer ${token}`;
    } else {
      delete apiClient.defaults.headers.common.Authorization;
    }

    if (typeof window !== 'undefined') {
      const tenantId = window.localStorage.getItem(tenantStorageKey);
      applyTenantHeader(tenantId);

      setState((previous) => {
        if (previous.isConfigured && previous.tenantId === tenantId) {
          return previous;
        }

        return { isConfigured: true, tenantId };
      });
      return;
    }

    setState((previous) => {
      if (previous.isConfigured) {
        return previous;
      }

      return { ...previous, isConfigured: true };
    });
  }, [token]);

  useEffect(() => {
    if (typeof window === 'undefined') {
      return;
    }

    ensureLocalStorageTenantBroadcast();

    const updateTenantId = (tenantId: string | null) => {
      applyTenantHeader(tenantId);

      setState((previous) => {
        if (previous.tenantId === tenantId) {
          return previous;
        }

        return { ...previous, tenantId };
      });
    };

    const handleStorage = (event: StorageEvent) => {
      if (event.key === tenantStorageKey) {
        updateTenantId(event.newValue);
      }
    };

    const handleCustomEvent = (event: Event) => {
      const tenantId = (event as TenantIdChangeEvent).detail?.tenantId ?? null;
      updateTenantId(tenantId);
    };

    window.addEventListener('storage', handleStorage);
    window.addEventListener(tenantChangeEventName, handleCustomEvent);

    return () => {
      window.removeEventListener('storage', handleStorage);
      window.removeEventListener(tenantChangeEventName, handleCustomEvent);
    };
  }, []);

  return state;
}
