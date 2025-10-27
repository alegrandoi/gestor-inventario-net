import clsx from 'clsx';
import { PropsWithChildren } from 'react';

interface BadgeProps extends PropsWithChildren {
  tone?: 'success' | 'warning' | 'neutral';
}

export function Badge({ children, tone = 'neutral' }: BadgeProps) {
  const tones: Record<string, string> = {
    success: 'bg-emerald-50 text-emerald-700 ring-emerald-600/20',
    warning: 'bg-amber-50 text-amber-700 ring-amber-600/20',
    neutral: 'bg-slate-100 text-slate-700 ring-slate-600/10'
  };

  return (
    <span className={clsx('inline-flex items-center rounded-full px-3 py-1 text-xs font-medium ring-1 ring-inset', tones[tone])}>
      {children}
    </span>
  );
}
