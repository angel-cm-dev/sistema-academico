import { ChevronLeft, ChevronRight } from 'lucide-react';
import { Button } from './Button';

interface PaginationProps {
  page: number;
  totalPages: number;
  totalCount: number;
  pageSize: number;
  onPageChange: (page: number) => void;
}

/** "Mostrando 11–20 de 57" + anterior/siguiente. */
export function Pagination({ page, totalPages, totalCount, pageSize, onPageChange }: PaginationProps) {
  if (totalCount === 0) return null;
  const desde = (page - 1) * pageSize + 1;
  const hasta = Math.min(page * pageSize, totalCount);

  return (
    <nav
      aria-label="Paginación"
      className="flex items-center justify-between gap-3 border-t border-slate-200 px-4 py-3 text-sm dark:border-slate-800"
    >
      <p className="text-slate-500">
        Mostrando <span className="font-medium text-slate-700 dark:text-slate-200">{desde}</span>–
        <span className="font-medium text-slate-700 dark:text-slate-200">{hasta}</span> de{' '}
        <span className="font-medium text-slate-700 dark:text-slate-200">{totalCount}</span>
      </p>
      <div className="flex items-center gap-2">
        <Button variant="secondary" size="sm" onClick={() => onPageChange(page - 1)} disabled={page <= 1} aria-label="Página anterior">
          <ChevronLeft className="h-4 w-4" />
        </Button>
        <span className="text-slate-600 dark:text-slate-300">
          {page} / {Math.max(totalPages, 1)}
        </span>
        <Button
          variant="secondary"
          size="sm"
          onClick={() => onPageChange(page + 1)}
          disabled={page >= totalPages}
          aria-label="Página siguiente"
        >
          <ChevronRight className="h-4 w-4" />
        </Button>
      </div>
    </nav>
  );
}
