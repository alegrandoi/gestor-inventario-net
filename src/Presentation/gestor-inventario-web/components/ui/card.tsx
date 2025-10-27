import { PropsWithChildren, ReactNode } from 'react';

interface CardProps extends PropsWithChildren {
  title: ReactNode;
  subtitle?: string;
  action?: ReactNode;
}

export function Card({ title, subtitle, action, children }: CardProps) {
  return (
    <section className="flex flex-col gap-5 rounded-2xl border border-slate-200 bg-white/80 p-6 shadow-sm backdrop-blur">
      <header className="grid gap-2 md:grid-cols-[minmax(0,1fr)_auto] md:items-start">
        <div className="flex flex-col gap-1">
          <h3 className="text-lg font-semibold text-slate-900">{title}</h3>
          {subtitle && <p className="text-sm text-slate-500">{subtitle}</p>}
        </div>
        {action && <div className="flex justify-start md:justify-end">{action}</div>}
      </header>
      <div className="text-sm text-slate-600">{children}</div>
    </section>
  );
}
