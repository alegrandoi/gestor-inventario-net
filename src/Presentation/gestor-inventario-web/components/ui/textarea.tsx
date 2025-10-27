import { forwardRef, ReactNode, TextareaHTMLAttributes } from 'react';
import clsx from 'clsx';

interface TextareaProps extends TextareaHTMLAttributes<HTMLTextAreaElement> {
  label?: ReactNode;
  hint?: string;
  error?: string;
}

export const Textarea = forwardRef<HTMLTextAreaElement, TextareaProps>(
  ({ label, hint, error, className, id, ...props }, ref) => {
    const textareaId = id ?? props.name ?? Math.random().toString(36).slice(2);

    return (
      <label className="flex w-full flex-col gap-1" htmlFor={textareaId}>
        {label && <span className="text-sm font-medium text-slate-700">{label}</span>}
        <textarea
          ref={ref}
          id={textareaId}
          className={clsx(
            'w-full rounded-lg border border-slate-200 bg-white px-3 py-2 text-sm text-slate-900 shadow-sm placeholder:text-slate-400 focus:border-primary-400 focus:outline-none focus:ring-2 focus:ring-primary-100',
            error && 'border-red-400 focus:ring-red-100',
            className
          )}
          {...props}
        />
        {hint && !error && <span className="text-xs text-slate-400">{hint}</span>}
        {error && <span className="text-xs text-red-500">{error}</span>}
      </label>
    );
  }
);

Textarea.displayName = 'Textarea';
