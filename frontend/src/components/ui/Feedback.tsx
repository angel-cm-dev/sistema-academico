import { AlertTriangle, Inbox, Loader2, RotateCw } from 'lucide-react';
import type { ReactNode } from 'react';
import { cn } from '@/lib/cn';
import { Button } from './Button';

export function Spinner({ className }: { className?: string }) {
  return <Loader2 aria-hidden className={cn('h-5 w-5 animate-spin text-brand-600', className)} />;
}

export function PageLoader({ label = 'Cargando…' }: { label?: string }) {
  return (
    <div role="status" className="flex min-h-[40vh] flex-col items-center justify-center gap-3 text-slate-500">
      <Spinner className="h-8 w-8" />
      <span className="text-sm">{label}</span>
    </div>
  );
}

export function EmptyState({ title, description, action }: { title: string; description?: string; action?: ReactNode }) {
  return (
    <div className="flex flex-col items-center justify-center gap-2 px-6 py-12 text-center">
      <Inbox aria-hidden className="h-10 w-10 text-slate-300 dark:text-slate-600" />
      <p className="font-medium text-slate-700 dark:text-slate-200">{title}</p>
      {description && <p className="max-w-md text-sm text-slate-500">{description}</p>}
      {action}
    </div>
  );
}

export function ErrorState({ message, onRetry }: { message: string; onRetry?: () => void }) {
  return (
    <div role="alert" className="flex flex-col items-center justify-center gap-3 px-6 py-12 text-center">
      <AlertTriangle aria-hidden className="h-10 w-10 text-amber-500" />
      <p className="max-w-md text-sm text-slate-600 dark:text-slate-300">{message}</p>
      {onRetry && (
        <Button variant="secondary" onClick={onRetry}>
          <RotateCw className="h-4 w-4" /> Reintentar
        </Button>
      )}
    </div>
  );
}

export function Alert({ kind = 'error', children }: { kind?: 'error' | 'success' | 'info'; children: ReactNode }) {
  const styles = {
    error: 'border-red-200 bg-red-50 text-red-700 dark:border-red-900 dark:bg-red-950/50 dark:text-red-300',
    success: 'border-emerald-200 bg-emerald-50 text-emerald-700 dark:border-emerald-900 dark:bg-emerald-950/50 dark:text-emerald-300',
    info: 'border-brand-200 bg-brand-50 text-brand-800 dark:border-brand-900 dark:bg-brand-950/50 dark:text-brand-200',
  }[kind];
  return (
    <div role={kind === 'error' ? 'alert' : 'status'} className={cn('rounded-lg border px-4 py-3 text-sm', styles)}>
      {children}
    </div>
  );
}
