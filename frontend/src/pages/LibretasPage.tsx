import { useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { ArrowLeft, Printer, Search } from 'lucide-react';
import { libretasApi, matriculasApi } from '@/api/endpoints';
import type { Libreta } from '@/api/types';
import { Button } from '@/components/ui/Button';
import { EmptyState, ErrorState, PageLoader } from '@/components/ui/Feedback';
import { Input } from '@/components/ui/Form';
import { Badge, Card, PageHeader } from '@/components/ui/Layout';
import { Table, Td, Th } from '@/components/ui/Table';
import { useAsync } from '@/hooks/useAsync';
import { useDebounce } from '@/hooks/useDebounce';
import { cn } from '@/lib/cn';
import { formatFecha, formatPorcentaje } from '@/lib/format';
import { colorNota } from '@/lib/notas';

export default function LibretasPage() {
  const { matriculaId } = useParams();
  return matriculaId ? <LibretaVista matriculaId={Number(matriculaId)} /> : <SelectorMatricula />;
}

/** Lista de matrículas visibles para el usuario (el backend ya filtra por rol). */
function SelectorMatricula() {
  const navigate = useNavigate();
  const [search, setSearch] = useState('');
  const termino = useDebounce(search);
  const { data, loading, error, reload } = useAsync(
    () => matriculasApi.listar({ search: termino, pageSize: 20, estado: 'Activa' }),
    [termino],
  );

  return (
    <>
      <PageHeader title="Libretas de notas" description="Seleccione un alumno para ver e imprimir su libreta." />
      <Card>
        <div className="border-b border-slate-200 p-4 dark:border-slate-800">
          <div className="relative">
            <Search className="pointer-events-none absolute left-3 top-2.5 h-4 w-4 text-slate-400" aria-hidden />
            <Input aria-label="Buscar alumno" placeholder="Buscar alumno…" className="pl-9" value={search} onChange={(e) => setSearch(e.target.value)} />
          </div>
        </div>
        {loading && !data ? (
          <PageLoader />
        ) : error ? (
          <ErrorState message={error.message} onRetry={reload} />
        ) : !data || data.items.length === 0 ? (
          <EmptyState title="No hay alumnos para mostrar" />
        ) : (
          <ul className="divide-y divide-slate-100 dark:divide-slate-800">
            {data.items.map((m) => (
              <li key={m.id}>
                <button
                  className="flex w-full items-center justify-between px-5 py-3 text-left hover:bg-slate-50 dark:hover:bg-slate-800/50"
                  onClick={() => navigate(`/libretas/${m.id}`)}
                >
                  <span>
                    <span className="font-medium">{m.alumno}</span>
                    <span className="ml-2 text-xs text-slate-500">{m.codigoAlumno}</span>
                  </span>
                  <span className="text-sm text-slate-500">
                    {m.seccion} · {m.anio}
                  </span>
                </button>
              </li>
            ))}
          </ul>
        )}
      </Card>
    </>
  );
}

function LibretaVista({ matriculaId }: { matriculaId: number }) {
  const { data, loading, error, reload } = useAsync(() => libretasApi.obtener(matriculaId), [matriculaId]);

  if (loading) return <PageLoader label="Generando libreta…" />;
  if (error || !data) return <ErrorState message={error?.message ?? 'Sin datos'} onRetry={reload} />;

  return (
    <>
      <div className="mb-4 flex items-center justify-between print:hidden">
        <Link to="/libretas" className="inline-flex items-center gap-1 text-sm text-brand-600 hover:underline">
          <ArrowLeft className="h-4 w-4" /> Volver
        </Link>
        <Button onClick={() => window.print()}>
          <Printer className="h-4 w-4" /> Imprimir / PDF
        </Button>
      </div>
      <LibretaDocumento libreta={data} />
    </>
  );
}

/** Documento imprimible. Usa clases print: para quitar sombras, colores de fondo y el tema oscuro. */
export function LibretaDocumento({ libreta: l }: { libreta: Libreta }) {
  return (
    <article className="mx-auto max-w-4xl rounded-xl border border-slate-200 bg-white p-6 text-slate-900 shadow-sm dark:border-slate-800 dark:bg-slate-900 dark:text-slate-100 print:max-w-none print:rounded-none print:border-0 print:bg-white print:p-0 print:text-black print:shadow-none">
      <header className="flex flex-col justify-between gap-4 border-b-2 border-slate-800 pb-4 dark:border-slate-300 print:border-black sm:flex-row">
        <div>
          <p className="text-xs uppercase tracking-widest text-slate-500 print:text-black">Institución Educativa Demo · Lima</p>
          <h1 className="text-xl font-bold">Libreta de notas {l.anio}</h1>
          <p className="text-sm">
            Nivel {l.nivel} · {l.grado} «{l.seccion}»
          </p>
        </div>
        <dl className="grid grid-cols-[auto_1fr] gap-x-3 text-sm">
          <dt className="text-slate-500 print:text-black">Alumno:</dt>
          <dd className="font-semibold">{l.alumno}</dd>
          <dt className="text-slate-500 print:text-black">Código:</dt>
          <dd>{l.codigoAlumno}</dd>
          <dt className="text-slate-500 print:text-black">DNI:</dt>
          <dd>{l.dni}</dd>
          <dt className="text-slate-500 print:text-black">Apoderado:</dt>
          <dd>{l.apoderado}</dd>
        </dl>
      </header>

      <section className="mt-4">
        <Table>
          <thead>
            <tr>
              <Th className="print:bg-white print:text-black">Área / Competencia</Th>
              {[1, 2, 3, 4].map((b) => (
                <Th key={b} className="text-center print:bg-white print:text-black">
                  B{b}
                </Th>
              ))}
              <Th className="text-center print:bg-white print:text-black">Final</Th>
            </tr>
          </thead>
          {l.cursos.length === 0 && (
            <tbody>
              <tr>
                <Td className="text-center text-slate-500">Sin cursos registrados para el grado.</Td>
              </tr>
            </tbody>
          )}
          {l.cursos.map((c) => (
            <tbody key={c.cursoId} className="break-inside-avoid border-b border-slate-200 dark:border-slate-700 print:border-black">
              <tr className="bg-slate-50 dark:bg-slate-800/60 print:bg-white">
                <Td className="font-semibold text-slate-900 dark:text-white print:text-black">
                  {c.curso}
                  {c.docente && <span className="block text-xs font-normal text-slate-500 print:text-black">Docente: {c.docente}</span>}
                </Td>
                {c.promediosBimestrales.map((p, i) => (
                  <Td key={i} className={cn('text-center', colorNota(p), 'print:text-black')}>
                    {p ?? '—'}
                  </Td>
                ))}
                <Td className="text-center">
                  {c.promedioFinal === null ? (
                    '—'
                  ) : (
                    <span className={cn('font-bold', colorNota(c.promedioFinal), 'print:text-black')}>
                      {c.promedioFinal} <span className="text-xs font-normal">({c.literal})</span>
                    </span>
                  )}
                </Td>
              </tr>
              {c.competencias.map((comp) => (
                <tr key={comp.id}>
                  <Td className="whitespace-normal pl-8 text-xs">
                    {comp.nombre} {comp.peso !== 1 && <span className="text-slate-400">(×{comp.peso})</span>}
                  </Td>
                  {comp.notas.map((n, i) => (
                    <Td key={i} className="text-center text-xs">
                      {n ?? '·'}
                    </Td>
                  ))}
                  <Td />
                </tr>
              ))}
            </tbody>
          ))}
        </Table>
      </section>

      <section className="mt-6 grid gap-4 sm:grid-cols-3 print:grid-cols-3">
        <div className="rounded-lg border border-slate-200 p-3 dark:border-slate-700 print:border-black">
          <p className="text-xs uppercase text-slate-500 print:text-black">Promedio general</p>
          <p className="text-2xl font-bold">{l.promedioGeneral ?? '—'}</p>
          <p className="text-xs">
            {l.cursosAprobados} aprobado(s) · {l.cursosDesaprobados} desaprobado(s)
          </p>
        </div>
        <div className="rounded-lg border border-slate-200 p-3 dark:border-slate-700 print:border-black">
          <p className="text-xs uppercase text-slate-500 print:text-black">Asistencia</p>
          <p className="text-2xl font-bold">{formatPorcentaje(l.asistencia.porcentaje)}</p>
          <p className="text-xs">
            {l.asistencia.presentes} P · {l.asistencia.tardanzas} T · {l.asistencia.faltas} F · {l.asistencia.faltasJustificadas} J
          </p>
          {l.asistencia.enRiesgo && (
            <span className="print:hidden">
              <Badge tone="red">Riesgo por inasistencia</Badge>
            </span>
          )}
        </div>
        <div className="rounded-lg border border-slate-200 p-3 dark:border-slate-700 print:border-black">
          <p className="text-xs uppercase text-slate-500 print:text-black">Situación</p>
          <p className="text-lg font-bold">{l.situacion}</p>
          <p className="text-xs">Emitido el {formatFecha(l.fechaEmision)}</p>
        </div>
      </section>

      <footer className="mt-6 text-xs text-slate-500 print:text-black">
        <p>
          Escala vigesimal 0-20. Nota aprobatoria: 11. Promedio bimestral ponderado por competencias y redondeado (10.5 → 11).
          Equivalencia: AD 18-20 · A 14-17 · B 11-13 · C 0-10.
        </p>
        <div className="mt-12 hidden grid-cols-2 gap-12 text-center print:grid">
          <p className="border-t border-black pt-1">Dirección</p>
          <p className="border-t border-black pt-1">Tutor(a)</p>
        </div>
      </footer>
    </article>
  );
}
