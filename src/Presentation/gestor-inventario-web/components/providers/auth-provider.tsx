'use client';

import { ReactNode, useEffect } from 'react';
import { useAuthStore } from '../../src/store/auth-store';

interface AuthProviderProps {
  children: ReactNode;
}

export function AuthProvider({ children }: AuthProviderProps) {
  const initialize = useAuthStore((state) => state.initialize);

  useEffect(() => {
    initialize().catch((error) => console.error('Error inicializando sesión', error));
  }, [initialize]);

  return <>{children}</>;
}
