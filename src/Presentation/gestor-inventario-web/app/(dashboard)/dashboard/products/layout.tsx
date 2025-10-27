'use client';

import Link from 'next/link';
import { usePathname } from 'next/navigation';
import clsx from 'clsx';

const sections = [
  { href: '/dashboard/products', label: 'Catálogo' },
  { href: '/dashboard/products/categories', label: 'Categorías' },
  { href: '/dashboard/products/attributes', label: 'Atributos' },
  { href: '/dashboard/products/tax-rates', label: 'Impuestos' }
] as const;

export default function ProductsLayout({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap gap-2">
        {sections.map((section) => {
          const isActive = pathname === section.href;
          return (
            <Link
              key={section.href}
              href={section.href}
              className={clsx(
                'rounded-full border px-4 py-2 text-sm font-medium transition',
                isActive
                  ? 'border-primary-600 bg-primary-600 text-white shadow-sm'
                  : 'border-slate-200 bg-white text-slate-600 hover:border-primary-200 hover:text-primary-600'
              )}
            >
              {section.label}
            </Link>
          );
        })}
      </div>
      {children}
    </div>
  );
}
