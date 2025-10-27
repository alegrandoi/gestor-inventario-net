'use client';

import { useRouter } from 'next/navigation';
import { Button } from '../ui/button';
import { useAuthStore } from '../../src/store/auth-store';

export function Topbar() {
  const router = useRouter();
  const { user, logout } = useAuthStore((state) => ({ user: state.user, logout: state.logout }));

  return (
    <header className="flex h-16 items-center justify-between border-b border-slate-200 bg-white/80 px-6 shadow-sm">
      <div>
        <h1 className="text-lg font-semibold text-slate-900">Panel de control</h1>
        <p className="text-xs text-slate-500">Supervisa KPIs de inventario, pedidos y previsiones.</p>
      </div>
      <div className="flex items-center gap-3">
        <div className="flex flex-col items-end text-right">
          <span className="text-sm font-semibold text-slate-900">{user?.username}</span>
          <span className="text-xs text-slate-500">{user?.role ?? 'Sin rol'}</span>
        </div>
        <Button
          variant="ghost"
          size="sm"
          onClick={() => {
            logout();
            router.replace('/login');
          }}
        >
          Cerrar sesión
        </Button>
      </div>
    </header>
  );
}
