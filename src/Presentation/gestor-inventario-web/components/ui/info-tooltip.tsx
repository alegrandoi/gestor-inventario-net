import { InformationCircleIcon } from '@heroicons/react/24/outline';
import { useState } from 'react';
import clsx from 'clsx';

type TooltipPosition = 'top' | 'bottom';

type TooltipSize = 'sm' | 'md';

interface InfoTooltipProps {
  content: string;
  className?: string;
  position?: TooltipPosition;
  size?: TooltipSize;
}

const positionClasses: Record<TooltipPosition, string> = {
  top: 'bottom-full mb-2 -translate-x-1/2',
  bottom: 'top-full mt-2 -translate-x-1/2'
};

const sizeClasses: Record<TooltipSize, string> = {
  sm: 'h-4 w-4',
  md: 'h-5 w-5'
};

export function InfoTooltip({
  content,
  className,
  position = 'top',
  size = 'md'
}: InfoTooltipProps) {
  const [isVisible, setIsVisible] = useState(false);

  function show() {
    setIsVisible(true);
  }

  function hide() {
    setIsVisible(false);
  }

  return (
    <span
      className={clsx('relative inline-flex items-center', className)}
      onMouseEnter={show}
      onMouseLeave={hide}
      onFocus={show}
      onBlur={hide}
      tabIndex={0}
      role="button"
      aria-label={content}
    >
      <InformationCircleIcon
        aria-hidden="true"
        className={clsx('text-primary-500 transition-colors hover:text-primary-600 focus-visible:text-primary-600', sizeClasses[size])}
      />
      <span
        role="tooltip"
        className={clsx(
          'pointer-events-none absolute left-1/2 z-30 w-60 max-w-xs rounded-lg border border-slate-200 bg-white px-3 py-2 text-xs font-medium text-slate-600 shadow-xl transition-opacity duration-150',
          positionClasses[position],
          isVisible ? 'opacity-100' : 'opacity-0'
        )}
      >
        {content}
      </span>
    </span>
  );
}
