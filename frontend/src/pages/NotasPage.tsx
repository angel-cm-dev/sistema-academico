import { useEffect, useMemo, useState } from 'react';
import { Save } from 'lucide-react';
import { ApiError } from '@/api/client';
import { estructuraApi, notasApi } from '@/api/endpoints';
import type { RegistroNotas } from '@/api/types';
import { Button } from '@/components/ui/Button';
import { Alert, EmptyState, ErrorState, PageLoader } from '@/components/ui/Feedback';
import { Field, Select } from '@/components/ui/Form';
import { Badge, Card, PageHeader } from '@/components/ui/Layout';
import { Td, Th } from '@/components/ui/Table';
import { useAsync } from '@/hooks/useAsync';
import { cn } from '@/lib/cn';
import { colorNota, parseNota, promedioPonderado, redondearVigesimal } from '@/lib/notas';

type Celdas = Record<string, string>; // clave "matriculaId-competenciaId" → texto del input
const clave = (m: number, c: number) => `${m}-${c}`;

function celdasDesde(registro: RegistroNotas): Celdas {
  const celdas: Celdas = {};
  for (const f of registro.filas) for (const n of f.notas) celdas[clave(f.matriculaId, n.competenciaId)] = n.valor?.toString() ?? '';
  return celdas;
}

export default function NotasPage() {
  const asignaciones = useAsync(() => estructuraApi.asignaciones(), []);
  const [asignacionId, setAsignacionId] = useState<number | ''>('');
  const [bimestre, setBimestre] = useState(1);
  const [celdas, setCeldas] = useState<Celdas>({});
  const [original, setOriginal] = useState<Celdas>({});
  const [mensaje, setMensaje] = useState<{ tipo: 'success' | 'error'; texto: string } | null>(null);
  const [guardando, setGuardando] = useState(false);

  useEffect(() => {
    if (asignacionId === '' && asignaciones.data?.length) setAsignacionId(asignaciones.data[0].id);
  }, [asignaciones.data, asignacionId]);

  const registro = useAsync(
    () => (asignacionId ? notasApi.registro(asignacionId, bimestre) : Promise.resolve(undefined)),
    [asignacionId, bimestre],
  );

  useEffect(() => {
    if (!registro.data) return;
    const c = celdasDesde(registro.data);
    setCeldas(c);
    setOriginal(c);
    setMensaje(null);
  }, [registro.data]);

  const cambios = useMemo(() => Object.keys(celdas).filter((k) => celdas[k] !== original[k]), [celdas, original]);
  const invalidas = useMemo(() => Object.keys(celdas).filter((k) => parseNota(celdas[k]) === 'invalida'), [celdas]);

  const guardar = async () => {
    if (!registro.data || !asignacionId) return;
    setGuardando(true);
    setMensaje(null);
    try {
      const notas = cambios.map((k) => {
        const [matriculaId, competenciaId] = k.split('-').map(Number);
        const v = parseNota(celdas[k]);
        return { matriculaId, competenciaId, valor: v === 'invalida' ? null : v };
      });
      const actualizado = await notasApi.guardar(asignacionId, bimestre, notas);
      registro.setData(actualizado);
      setMensaje({ tipo: 'success', texto: `Se guardaron ${notas.length} nota(s).` });
    } catch (e) {
      setMensaje({ tipo: 'error', texto: e instanceof ApiError ? e.message : 'No se pudo guardar.' });
    } finally {
      setGuardando(false);
    }
  };

  if (asignaciones.loading) return <PageLoader />;
  if (asignaciones.error) return <ErrorState message={asignaciones.error.message} onRetry={asignaciones.reload} />;
  if (!asignaciones.data?.length) return <EmptyState title="No tiene cursos asignados" description="Solicite a la administración su carga académica." />;

  const r = registro.data;
  return (
    <>
      <PageHeader
        title="Registro de notas"
        description="Notas por competencia (0-20). El promedio del bimestre es ponderado según los pesos del curso."
        actions={
          <Button onClick={guardar} loading={guardando} disabled={cambios.length === 0 || invalidas.length > 0}>
            <Save className="h-4 w-4" /> Guardar {cambios.length > 0 && `(${cambios.length})`}
          </Button>
        }
      />

      <Card className="mb-4 p-4">
        <div className="grid gap-4 sm:grid-cols-[1fr_12rem]">
          <Field label="Curso y sección">
            {(id) => (
              <Select id={id} value={asignacionId} onChange={(e) => setAsignacionId(Number(e.target.value))}>
                {asignaciones.data?.map((a) => (
                  <option key={a.id} value={a.id}>
                    {a.curso} — {a.seccion}
                  </option>
                ))}
              </Select>
            )}
          </Field>
          <Field label="Bimestre">
            {(id) => (
              <Select id={id} value={bimestre} onChange={(e) => setBimestre(Number(e.target.value))}>
                {[1, 2, 3, 4].map((b) => (
                  <option key={b} value={b}>
                    {b}° bimestre
                  </option>
                ))}
              </Select>
            )}
          </Field>
        </div>
      </Card>

      {mensaje && (
        <div className="mb-4">
          <Alert kind={mensaje.tipo}>{mensaje.texto}</Alert>
        </div>
      )}
      {invalidas.length > 0 && (
        <div className="mb-4">
          <Alert>Hay {invalidas.length} nota(s) fuera de la escala 0-20.</Alert>
        </div>
      )}

      <Card>
        {registro.loading && !r ? (
          <PageLoader />
        ) : registro.error ? (
          <ErrorState message={registro.error.message} onRetry={registro.reload} />
        ) : !r || r.filas.length === 0 ? (
          <EmptyState title="No hay alumnos matriculados en esta sección" />
        ) : (
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-slate-200 text-sm dark:divide-slate-800">
              <thead>
                <tr>
                  <Th className="sticky left-0 z-10">Alumno</Th>
                  {r.competencias.map((c, i) => (
                    <Th key={c.id} className="text-center">
                      <span title={c.nombre}>C{i + 1}</span>
                      <span className="block font-normal normal-case text-slate-400">peso {c.peso}</span>
                    </Th>
                  ))}
                  <Th className="text-center">Promedio</Th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100 dark:divide-slate-800">
                {r.filas.map((f) => {
                  const valores = r.competencias.map((c) => {
                    const v = parseNota(celdas[clave(f.matriculaId, c.id)] ?? '');
                    return { valor: v === 'invalida' ? null : v, peso: c.peso };
                  });
                  const exacto = promedioPonderado(valores);
                  const prom = exacto === null ? null : redondearVigesimal(exacto);
                  return (
                    <tr key={f.matriculaId}>
                      <Td className="sticky left-0 bg-white font-medium dark:bg-slate-900">{f.alumno}</Td>
                      {r.competencias.map((c) => {
                        const k = clave(f.matriculaId, c.id);
                        const invalida = parseNota(celdas[k] ?? '') === 'invalida';
                        return (
                          <Td key={c.id} className="px-2 text-center">
                            <input
                              aria-label={`${f.alumno} — ${c.nombre}`}
                              inputMode="decimal"
                              className={cn(
                                'w-16 rounded-md border px-2 py-1 text-center text-sm dark:bg-slate-900',
                                invalida ? 'border-red-400 bg-red-50 dark:bg-red-950' : 'border-slate-300 dark:border-slate-700',
                                celdas[k] !== original[k] && !invalida && 'border-brand-400 bg-brand-50 dark:bg-brand-950/40',
                              )}
                              value={celdas[k] ?? ''}
                              onChange={(e) => setCeldas({ ...celdas, [k]: e.target.value })}
                            />
                          </Td>
                        );
                      })}
                      <Td className="text-center">
                        {prom === null ? (
                          <span className="text-slate-400">—</span>
                        ) : (
                          <span className="inline-flex items-center gap-2">
                            <span className={cn('text-base', colorNota(prom))} title={`Exacto: ${exacto}`}>
                              {prom}
                            </span>
                            <Badge tone={prom >= 11 ? 'green' : 'red'}>{prom >= 11 ? 'Aprobado' : 'Desaprobado'}</Badge>
                          </span>
                        )}
                      </Td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
            <div className="border-t border-slate-200 px-4 py-3 text-xs text-slate-500 dark:border-slate-800">
              {r.competencias.map((c, i) => (
                <p key={c.id}>
                  <strong>C{i + 1}:</strong> {c.nombre}
                </p>
              ))}
            </div>
          </div>
        )}
      </Card>
    </>
  );
}
