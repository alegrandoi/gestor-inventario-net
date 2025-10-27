'use client';

import Link from 'next/link';
import type { Route } from 'next';
import { usePathname } from 'next/navigation';
import {
  BuildingOffice2Icon,
  ChartBarIcon,
  ClipboardDocumentListIcon,
  Cog6ToothIcon,
  CubeIcon,
  UserGroupIcon,
  HomeModernIcon,
  InboxStackIcon,
  TruckIcon
} from '@heroicons/react/24/outline';
import clsx from 'clsx';

type NavItem = {
  href: Route;
  label: string;
  icon: typeof ChartBarIcon;
};

const navItems: NavItem[] = [
  { href: '/dashboard', label: 'Resumen', icon: ChartBarIcon },
  { href: '/dashboard/warehouses', label: 'Almacén', icon: BuildingOffice2Icon },
  { href: '/dashboard/contacts', label: 'Contactos', icon: UserGroupIcon },
  { href: '/dashboard/products', label: 'Productos', icon: CubeIcon },
  { href: '/dashboard/orders', label: 'Pedidos', icon: ClipboardDocumentListIcon },
  { href: '/dashboard/inventory', label: 'Inventario', icon: InboxStackIcon },
  { href: '/dashboard/logistics', label: 'Logística', icon: TruckIcon },
  { href: '/dashboard/planning', label: 'Planificación', icon: HomeModernIcon },
  { href: '/dashboard/admin', label: 'Administración', icon: Cog6ToothIcon }
];

export function Sidebar() {
  const pathname = usePathname();

  return (
    <aside className="hidden w-64 flex-col border-r border-slate-200 bg-white/80 px-4 py-6 shadow-sm lg:flex">
      <div className="mb-8 flex items-center gap-2">
        <span className="flex h-10 w-10 items-center justify-center rounded-xl bg-primary-100 text-primary-600">
          <ChartBarIcon className="h-6 w-6" />
        </span>
        <div>
          <p className="text-sm font-semibold text-primary-600">Gestor Inventario</p>
          <p className="text-xs text-slate-500">Control unificado</p>
        </div>
      </div>

      <nav className="flex flex-1 flex-col gap-1">
        {navItems.map((item) => {
          const isActive = pathname === item.href;
          const Icon = item.icon;

          return (
            <Link
              key={item.href}
              href={item.href}
              className={clsx(
                'flex items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium transition',
                isActive ? 'bg-primary-600 text-white shadow-sm' : 'text-slate-600 hover:bg-slate-100'
              )}
            >
              <Icon className="h-5 w-5" />
              {item.label}
            </Link>
          );
        })}
      </nav>
    </aside>
  );
}
