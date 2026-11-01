import { forwardRef, useId, type InputHTMLAttributes, type ReactNode, type SelectHTMLAttributes } from 'react';
import { cn } from '@/lib/cn';

const controlBase =
  'block w-full rounded-lg border bg-white px-3 py-2 text-sm text-slate-900 shadow-sm transition placeholder:text-slate-400 focus:outline-none focus:ring-2 disabled:bg-slate-100 dark:bg-slate-900 dark:text-slate-100 dark:disabled:bg-slate-800';
const controlOk = 'border-slate-300 focus:border-brand-500 focus:ring-brand-500/30 dark:border-slate-700';
const controlError = 'border-red-400 focus:border-red-500 focus:ring-red-500/30';

interface FieldProps {
  label: string;
  error?: string;
  hint?: string;
  children: (id: string, describedBy: string | undefined) => ReactNode;
}

/** Envuelve un control con label, ayuda y mensaje de error accesibles (aria-describedby). */
export function Field({ label, error, hint, children }: FieldProps) {
  const id = useId();
  const msgId = `${id}-msg`;
  return (
    <div className="space-y-1">
      <label htmlFor={id} className="block text-sm font-medium text-slate-700 dark:text-slate-300">
        {label}
      </label>
      {children(id, error || hint ? msgId : undefined)}
      {error ? (
        <p id={msgId} className="text-xs text-red-600 dark:text-red-400">
          {error}
        </p>
      ) : (
        hint && (
          <p id={msgId} className="text-xs text-slate-500">
            {hint}
          </p>
        )
      )}
    </div>
  );
}

export const Input = forwardRef<HTMLInputElement, InputHTMLAttributes<HTMLInputElement> & { invalid?: boolean }>(
  function Input({ className, invalid, ...props }, ref) {
    return (
      <input
        ref={ref}
        aria-invalid={invalid || undefined}
        className={cn(controlBase, invalid ? controlError : controlOk, className)}
        {...props}
      />
    );
  },
);

export const Select = forwardRef<HTMLSelectElement, SelectHTMLAttributes<HTMLSelectElement> & { invalid?: boolean }>(
  function Select({ className, invalid, children, ...props }, ref) {
    return (
      <select
        ref={ref}
        aria-invalid={invalid || undefined}
        className={cn(controlBase, invalid ? controlError : controlOk, 'pr-8', className)}
        {...props}
      >
        {children}
      </select>
    );
  },
);
