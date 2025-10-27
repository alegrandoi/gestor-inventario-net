import { forwardRef, ReactNode, SelectHTMLAttributes } from 'react';
import clsx from 'clsx';

interface SelectProps extends SelectHTMLAttributes<HTMLSelectElement> {
  label?: ReactNode;
  hint?: string;
  error?: string;
}

export const Select = forwardRef<HTMLSelectElement, SelectProps>(
  ({ label, hint, error, className, id, children, ...props }, ref) => {
    const selectId = id ?? props.name ?? Math.random().toString(36).slice(2);

    return (
      <label className="flex w-full flex-col gap-1" htmlFor={selectId}>
        {label && <span className="text-sm font-medium text-slate-700">{label}</span>}
        <select
          ref={ref}
          id={selectId}
          className={clsx(
            'w-full rounded-lg border border-slate-200 bg-white px-3 py-2 text-sm text-slate-900 shadow-sm focus:border-primary-400 focus:outline-none focus:ring-2 focus:ring-primary-100',
            error && 'border-red-400 focus:ring-red-100',
            className
          )}
          {...props}
        >
          {children}
        </select>
        <div className="min-h-[1.25rem]">
          {hint && !error && <span className="text-xs text-slate-400">{hint}</span>}
          {error && <span className="text-xs text-red-500">{error}</span>}
        </div>
      </label>
    );
  }
);

Select.displayName = 'Select';
