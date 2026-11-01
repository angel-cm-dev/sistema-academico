import type { ReactNode } from 'react';
import { ArrowDown, ArrowUp, ArrowUpDown } from 'lucide-react';
import { cn } from '@/lib/cn';

export function Table({ children }: { children: ReactNode }) {
  return (
    <div className="overflow-x-auto">
      <table className="min-w-full divide-y divide-slate-200 text-sm dark:divide-slate-800">{children}</table>
    </div>
  );
}

export function Th({
  children,
  className,
  ariaSort,
}: {
  children?: ReactNode;
  className?: string;
  ariaSort?: 'ascending' | 'descending' | 'none';
}) {
  return (
    <th
      scope="col"
      aria-sort={ariaSort}
      className={cn(
        'whitespace-nowrap bg-slate-50 px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500 dark:bg-slate-800/50 dark:text-slate-400',
        className,
      )}
    >
      {children}
    </th>
  );
}

/** Encabezado ordenable: alterna asc/desc y muestra el estado con un ícono. */
export function SortableTh({
  label,
  field,
  sortBy,
  sortDir,
  onSort,
}: {
  label: string;
  field: string;
  sortBy: string;
  sortDir: 'asc' | 'desc';
  onSort: (field: string) => void;
}) {
  const activo = sortBy === field;
  const Icon = !activo ? ArrowUpDown : sortDir === 'asc' ? ArrowUp : ArrowDown;
  return (
    <Th ariaSort={activo ? (sortDir === 'asc' ? 'ascending' : 'descending') : 'none'}>
      <button
        type="button"
        onClick={() => onSort(field)}
        className="inline-flex items-center gap-1 uppercase hover:text-slate-800 dark:hover:text-slate-200"
      >
        {label}
        <Icon className={cn('h-3.5 w-3.5', !activo && 'opacity-40')} />
      </button>
    </Th>
  );
}

export function Td({ children, className }: { children?: ReactNode; className?: string }) {
  return <td className={cn('whitespace-nowrap px-4 py-3 text-slate-700 dark:text-slate-300', className)}>{children}</td>;
}
