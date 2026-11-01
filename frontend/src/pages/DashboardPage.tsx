import { Banknote, CalendarCheck, GraduationCap, Percent, TrendingUp, Users } from 'lucide-react';
import { dashboardApi } from '@/api/endpoints';
import { useAuth } from '@/auth/AuthContext';
import { ErrorState, PageLoader, EmptyState } from '@/components/ui/Feedback';
import { Card, PageHeader } from '@/components/ui/Layout';
import { useAsync } from '@/hooks/useAsync';
import { cn } from '@/lib/cn';
import { formatKpi } from '@/lib/format';

const ICONOS: Record<string, typeof Users> = {
  alumnos: Users,
  matriculas: GraduationCap,
  ocupacion: Percent,
  recaudado: TrendingUp,
  vencido: Banknote,
  asistencia: CalendarCheck,
};

export default function DashboardPage() {
  const { usuario } = useAuth();
  const { data, loading, error, reload } = useAsync(() => dashboardApi.obtener(), []);

  if (loading) return <PageLoader />;
  if (error || !data) return <ErrorState message={error?.message ?? 'Sin datos'} onRetry={reload} />;

  return (
    <>
      <PageHeader
        title={`Hola, ${usuario?.nombreCompleto.split(' ')[0]}`}
        description={data.anio ? `Resumen del año escolar ${data.anio}` : 'No hay un periodo académico activo.'}
      />

      <section aria-label="Indicadores" className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
        {data.kpis.map((k) => {
          const Icon = ICONOS[k.clave] ?? TrendingUp;
          const alerta = k.clave === 'vencido' && k.valor > 0;
          return (
            <Card key={k.clave} className="p-5">
              <div className="flex items-start justify-between">
                <div>
                  <p className="text-sm text-slate-500 dark:text-slate-400">{k.titulo}</p>
                  <p className={cn('mt-2 text-3xl font-semibold tracking-tight', alerta && 'text-red-600 dark:text-red-400')}>
                    {formatKpi(k.valor, k.formato)}
                  </p>
                  {k.detalle && <p className="mt-1 text-xs text-slate-500">{k.detalle}</p>}
                </div>
                <span
                  className={cn(
                    'rounded-lg p-2',
                    alerta ? 'bg-red-50 text-red-600 dark:bg-red-950' : 'bg-brand-50 text-brand-600 dark:bg-brand-950 dark:text-brand-300',
                  )}
                >
                  <Icon className="h-5 w-5" aria-hidden />
                </span>
              </div>
            </Card>
          );
        })}
      </section>

      {data.rol !== 'Apoderado' && (
        <Card className="mt-6">
          <div className="border-b border-slate-200 px-5 py-4 dark:border-slate-800">
            <h2 className="font-semibold">Ocupación por sección</h2>
            <p className="text-sm text-slate-500">Vacantes usadas respecto a la capacidad de cada aula.</p>
          </div>
          {data.secciones.length === 0 ? (
            <EmptyState title="Sin secciones en el periodo activo" />
          ) : (
            <ul className="divide-y divide-slate-100 dark:divide-slate-800">
              {data.secciones.map((s) => (
                <li key={s.seccionId} className="flex items-center gap-4 px-5 py-3">
                  <span className="w-40 shrink-0 text-sm font-medium">{s.seccion}</span>
                  <div className="h-2.5 flex-1 overflow-hidden rounded-full bg-slate-100 dark:bg-slate-800" aria-hidden>
                    <div
                      className={cn('h-full rounded-full', s.ocupacion >= 100 ? 'bg-red-500' : s.ocupacion >= 85 ? 'bg-amber-500' : 'bg-brand-500')}
                      style={{ width: `${Math.min(100, s.ocupacion)}%` }}
                    />
                  </div>
                  <span className="w-28 shrink-0 text-right text-sm text-slate-600 dark:text-slate-300">
                    {s.matriculados}/{s.capacidad} · {s.ocupacion}%
                  </span>
                </li>
              ))}
            </ul>
          )}
        </Card>
      )}
    </>
  );
}
