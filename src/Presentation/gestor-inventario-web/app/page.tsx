'use client';

import { useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { useAuthStore } from '../src/store/auth-store';

export default function RootPage() {
  const token = useAuthStore((state) => state.token);
  const router = useRouter();

  useEffect(() => {
    router.replace(token ? '/dashboard' : '/login');
  }, [router, token]);

  return (
    <div className="flex flex-1 items-center justify-center">
      <div className="text-center text-sm text-slate-500">
        Redirigiendo a la experiencia adecuada…
      </div>
    </div>
  );
}
