import { useState } from 'react';
import { Save, SlidersHorizontal } from 'lucide-react';
import { ApiError } from '@/api/client';
import { estructuraApi } from '@/api/endpoints';
import type { Curso } from '@/api/types';
import { Button } from '@/components/ui/Button';
import { Alert, ErrorState, PageLoader } from '@/components/ui/Feedback';
import { Input } from '@/components/ui/Form';
import { Badge, Card, Modal, PageHeader } from '@/components/ui/Layout';
import { Table, Td, Th } from '@/components/ui/Table';
import { useAsync } from '@/hooks/useAsync';

export default function SeccionesPage() {
  const secciones = useAsync(() => estructuraApi.secciones(), []);
  const cursos = useAsync(() => estructuraApi.cursos(), []);
  const [editando, setEditando] = useState<Curso | null>(null);

  return (
    <>
      <PageHeader title="Secciones y cursos" description="Capacidad de aulas y configuración del promedio ponderado por competencias." />

      <div className="grid gap-6 xl:grid-cols-2">
        <Card>
          <h2 className="border-b border-slate-200 px-5 py-4 font-semibold dark:border-slate-800">Secciones del periodo</h2>
          {secciones.loading ? (
            <PageLoader />
          ) : secciones.error ? (
            <ErrorState message={secciones.error.message} onRetry={secciones.reload} />
          ) : (
            <Table>
              <thead>
                <tr>
                  <Th>Sección</Th>
                  <Th>Capacidad</Th>
                  <Th>Matriculados</Th>
                  <Th>Vacantes</Th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100 dark:divide-slate-800">
                {secciones.data?.map((s) => (
                  <tr key={s.id}>
                    <Td className="font-medium">
                      {s.grado} {s.letra}
                    </Td>
                    <Td>{s.capacidad}</Td>
                    <Td>{s.matriculados}</Td>
                    <Td>{s.vacantes === 0 ? <Badge tone="red">Llena</Badge> : <Badge tone="green">{s.vacantes} libres</Badge>}</Td>
                  </tr>
                ))}
              </tbody>
            </Table>
          )}
        </Card>

        <Card>
          <h2 className="border-b border-slate-200 px-5 py-4 font-semibold dark:border-slate-800">Cursos y pesos de competencias</h2>
          {cursos.loading ? (
            <PageLoader />
          ) : cursos.error ? (
            <ErrorState message={cursos.error.message} onRetry={cursos.reload} />
          ) : (
            <ul className="divide-y divide-slate-100 dark:divide-slate-800">
              {cursos.data?.map((c) => (
                <li key={c.id} className="flex items-start justify-between gap-3 px-5 py-3">
                  <div>
                    <p className="font-medium">
                      {c.nombre} <span className="text-xs text-slate-500">· {c.grado} · {c.codigo}</span>
                    </p>
                    <p className="mt-1 text-xs text-slate-500">
                      {c.competencias.map((x) => `${x.nombre.split(' ').slice(0, 4).join(' ')}… (×${x.peso})`).join(' · ')}
                    </p>
                  </div>
                  <Button variant="secondary" size="sm" onClick={() => setEditando(c)}>
                    <SlidersHorizontal className="h-3.5 w-3.5" /> Pesos
                  </Button>
                </li>
              ))}
            </ul>
          )}
        </Card>
      </div>

      {editando && (
        <PesosModal
          curso={editando}
          onClose={() => setEditando(null)}
          onSaved={() => {
            setEditando(null);
            cursos.reload();
          }}
        />
      )}
    </>
  );
}

function PesosModal({ curso, onClose, onSaved }: { curso: Curso; onClose: () => void; onSaved: () => void }) {
  const [pesos, setPesos] = useState<Record<number, string>>(Object.fromEntries(curso.competencias.map((c) => [c.id, String(c.peso)])));
  const [error, setError] = useState<string | null>(null);
  const [guardando, setGuardando] = useState(false);

  const valores = curso.competencias.map((c) => Number(pesos[c.id]));
  const invalido = valores.some((v) => !Number.isFinite(v) || v <= 0 || v > 100);
  const total = valores.reduce((a, b) => a + (Number.isFinite(b) ? b : 0), 0);

  const guardar = async () => {
    setGuardando(true);
    setError(null);
    try {
      await estructuraApi.actualizarCompetencias(
        curso.id,
        curso.competencias.map((c) => ({ id: c.id, nombre: c.nombre, peso: Number(pesos[c.id]) })),
      );
      onSaved();
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo guardar.');
    } finally {
      setGuardando(false);
    }
  };

  return (
    <Modal
      open
      title={`Pesos · ${curso.nombre} (${curso.grado})`}
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            Cancelar
          </Button>
          <Button onClick={guardar} loading={guardando} disabled={invalido}>
            <Save className="h-4 w-4" /> Guardar
          </Button>
        </>
      }
    >
      <div className="space-y-3">
        <Alert kind="info">
          El promedio del bimestre es Σ(nota × peso) / Σ(pesos). Un peso 2 hace que esa competencia valga el doble.
        </Alert>
        {error && <Alert>{error}</Alert>}
        {curso.competencias.map((c) => (
          <div key={c.id} className="flex items-center gap-3">
            <label htmlFor={`peso-${c.id}`} className="flex-1 text-sm">
              {c.nombre}
            </label>
            <Input
              id={`peso-${c.id}`}
              type="number"
              min={0.5}
              step={0.5}
              className="w-24"
              value={pesos[c.id]}
              invalid={!(Number(pesos[c.id]) > 0)}
              onChange={(e) => setPesos({ ...pesos, [c.id]: e.target.value })}
            />
            <span className="w-14 text-right text-xs text-slate-500">
              {total > 0 && Number(pesos[c.id]) > 0 ? `${((Number(pesos[c.id]) / total) * 100).toFixed(0)} %` : '—'}
            </span>
          </div>
        ))}
      </div>
    </Modal>
  );
}
