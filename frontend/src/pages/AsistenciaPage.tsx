import { useEffect, useState } from 'react';
import { CheckCheck, Save } from 'lucide-react';
import { ApiError } from '@/api/client';
import { asistenciaApi, estructuraApi } from '@/api/endpoints';
import type { EstadoAsistencia } from '@/api/types';
import { Button } from '@/components/ui/Button';
import { Alert, EmptyState, ErrorState, PageLoader } from '@/components/ui/Feedback';
import { Field, Input, Select } from '@/components/ui/Form';
import { Card, PageHeader } from '@/components/ui/Layout';
import { Table, Td, Th } from '@/components/ui/Table';
import { useAsync } from '@/hooks/useAsync';
import { cn } from '@/lib/cn';
import { hoyIso } from '@/lib/format';

const ESTADOS: { valor: EstadoAsistencia; etiqueta: string; clase: string }[] = [
  { valor: 'Presente', etiqueta: 'P', clase: 'bg-emerald-600 text-white' },
  { valor: 'Tarde', etiqueta: 'T', clase: 'bg-amber-500 text-white' },
  { valor: 'Falta', etiqueta: 'F', clase: 'bg-red-600 text-white' },
  { valor: 'FaltaJustificada', etiqueta: 'J', clase: 'bg-sky-600 text-white' },
];

interface Fila {
  estado: EstadoAsistencia | null;
  observacion: string;
}

export default function AsistenciaPage() {
  const secciones = useAsync(() => estructuraApi.secciones(), []);
  const [seccionId, setSeccionId] = useState<number | ''>('');
  const [fecha, setFecha] = useState(hoyIso());
  const [filas, setFilas] = useState<Record<number, Fila>>({});
  const [mensaje, setMensaje] = useState<{ tipo: 'success' | 'error'; texto: string } | null>(null);
  const [guardando, setGuardando] = useState(false);

  useEffect(() => {
    if (seccionId === '' && secciones.data?.length) setSeccionId(secciones.data[0].id);
  }, [secciones.data, seccionId]);

  const registro = useAsync(
    () => (seccionId ? asistenciaApi.registro(seccionId, fecha) : Promise.resolve(undefined)),
    [seccionId, fecha],
  );

  useEffect(() => {
    if (!registro.data) return;
    setFilas(Object.fromEntries(registro.data.filas.map((f) => [f.matriculaId, { estado: f.estado, observacion: f.observacion ?? '' }])));
    setMensaje(null);
  }, [registro.data]);

  const pendientes = Object.values(filas).filter((f) => f.estado === null).length;

  const marcarTodos = () =>
    setFilas(Object.fromEntries(Object.entries(filas).map(([id, f]) => [id, { ...f, estado: f.estado ?? 'Presente' }])));

  const guardar = async () => {
    if (!seccionId) return;
    setGuardando(true);
    setMensaje(null);
    try {
      const registros = Object.entries(filas)
        .filter(([, f]) => f.estado !== null)
        .map(([id, f]) => ({ matriculaId: Number(id), estado: f.estado as EstadoAsistencia, observacion: f.observacion || null }));
      const r = await asistenciaApi.guardar(seccionId, fecha, registros);
      registro.setData(r);
      setMensaje({ tipo: 'success', texto: `Asistencia guardada (${registros.length} alumnos).` });
    } catch (e) {
      setMensaje({ tipo: 'error', texto: e instanceof ApiError ? e.message : 'No se pudo guardar.' });
    } finally {
      setGuardando(false);
    }
  };

  if (secciones.loading) return <PageLoader />;
  if (secciones.error) return <ErrorState message={secciones.error.message} onRetry={secciones.reload} />;
  if (!secciones.data?.length) return <EmptyState title="No tiene secciones asignadas" />;

  return (
    <>
      <PageHeader
        title="Asistencia"
        description="P = presente, T = tardanza (cuenta como asistencia), F = falta, J = falta justificada (no penaliza)."
        actions={
          <>
            <Button variant="secondary" onClick={marcarTodos} disabled={pendientes === 0}>
              <CheckCheck className="h-4 w-4" /> Todos presentes
            </Button>
            <Button onClick={guardar} loading={guardando}>
              <Save className="h-4 w-4" /> Guardar
            </Button>
          </>
        }
      />

      <Card className="mb-4 p-4">
        <div className="grid gap-4 sm:grid-cols-2">
          <Field label="Sección">
            {(id) => (
              <Select id={id} value={seccionId} onChange={(e) => setSeccionId(Number(e.target.value))}>
                {secciones.data?.map((s) => (
                  <option key={s.id} value={s.id}>
                    {s.grado} {s.letra}
                  </option>
                ))}
              </Select>
            )}
          </Field>
          <Field label="Fecha">
            {(id) => <Input id={id} type="date" max={hoyIso()} value={fecha} onChange={(e) => setFecha(e.target.value)} />}
          </Field>
        </div>
      </Card>

      {mensaje && (
        <div className="mb-4">
          <Alert kind={mensaje.tipo}>{mensaje.texto}</Alert>
        </div>
      )}

      <Card>
        {registro.loading && !registro.data ? (
          <PageLoader />
        ) : registro.error ? (
          <ErrorState message={registro.error.message} onRetry={registro.reload} />
        ) : !registro.data || registro.data.filas.length === 0 ? (
          <EmptyState title="No hay alumnos matriculados en esta sección" />
        ) : (
          <Table>
            <thead>
              <tr>
                <Th>Alumno</Th>
                <Th>Estado</Th>
                <Th>Observación</Th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100 dark:divide-slate-800">
              {registro.data.filas.map((f) => {
                const actual = filas[f.matriculaId];
                return (
                  <tr key={f.matriculaId}>
                    <Td className="font-medium">{f.alumno}</Td>
                    <Td>
                      <div className="flex gap-1" role="radiogroup" aria-label={`Asistencia de ${f.alumno}`}>
                        {ESTADOS.map((e) => (
                          <button
                            key={e.valor}
                            type="button"
                            role="radio"
                            aria-checked={actual?.estado === e.valor}
                            title={e.valor}
                            onClick={() => setFilas({ ...filas, [f.matriculaId]: { ...actual, estado: e.valor } })}
                            className={cn(
                              'h-8 w-8 rounded-md text-xs font-bold transition',
                              actual?.estado === e.valor ? e.clase : 'bg-slate-100 text-slate-500 hover:bg-slate-200 dark:bg-slate-800',
                            )}
                          >
                            {e.etiqueta}
                          </button>
                        ))}
                      </div>
                    </Td>
                    <Td>
                      <Input
                        aria-label={`Observación de ${f.alumno}`}
                        className="min-w-[12rem]"
                        maxLength={250}
                        value={actual?.observacion ?? ''}
                        onChange={(e) => setFilas({ ...filas, [f.matriculaId]: { ...actual, observacion: e.target.value } })}
                      />
                    </Td>
                  </tr>
                );
              })}
            </tbody>
          </Table>
        )}
      </Card>
    </>
  );
}
