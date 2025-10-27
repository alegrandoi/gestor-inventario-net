import { ButtonHTMLAttributes, forwardRef } from 'react';
import clsx from 'clsx';

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: 'primary' | 'secondary' | 'outline' | 'ghost';
  size?: 'sm' | 'md';
}

export const Button = forwardRef<HTMLButtonElement, ButtonProps>(
  ({ className, children, variant = 'primary', size = 'md', ...props }, ref) => {
    const base = 'inline-flex items-center justify-center rounded-lg font-medium transition focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 focus-visible:ring-offset-2 disabled:opacity-60 disabled:cursor-not-allowed';

    const variants: Record<string, string> = {
      primary: 'bg-primary-600 text-white hover:bg-primary-700 shadow-sm',
      secondary: 'bg-white text-primary-700 border border-primary-200 hover:bg-primary-50',
      outline: 'border border-slate-200 bg-transparent text-slate-700 hover:bg-slate-100',
      ghost: 'text-slate-600 hover:bg-slate-100'
    };

    const sizes: Record<string, string> = {
      sm: 'px-3 py-1.5 text-sm',
      md: 'px-4 py-2 text-sm'
    };

    return (
      <button ref={ref} className={clsx(base, variants[variant], sizes[size], className)} {...props}>
        {children}
      </button>
    );
  }
);

Button.displayName = 'Button';
