import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { NotebookText, Plus, Search, UserMinus } from 'lucide-react';
import { ApiError } from '@/api/client';
import { alumnosApi, estructuraApi, matriculasApi } from '@/api/endpoints';
import type { Alumno, EstadoMatricula, MatriculaCreada } from '@/api/types';
import { Button } from '@/components/ui/Button';
import { Alert, EmptyState, ErrorState, PageLoader } from '@/components/ui/Feedback';
import { Field, Input, Select } from '@/components/ui/Form';
import { Badge, Card, Modal, PageHeader } from '@/components/ui/Layout';
import { Pagination } from '@/components/ui/Pagination';
import { SortableTh, Table, Td, Th } from '@/components/ui/Table';
import { useAsync } from '@/hooks/useAsync';
import { useDebounce } from '@/hooks/useDebounce';
import { formatFecha, formatMoneda } from '@/lib/format';

export default function MatriculasPage() {
  const [search, setSearch] = useState('');
  const [seccionId, setSeccionId] = useState<number | ''>('');
  const [estado, setEstado] = useState<EstadoMatricula | ''>('Activa');
  const [page, setPage] = useState(1);
  const [sortBy, setSortBy] = useState('alumno');
  const [sortDir, setSortDir] = useState<'asc' | 'desc'>('asc');
  const [creando, setCreando] = useState(false);
  const [aviso, setAviso] = useState<string | null>(null);
  const termino = useDebounce(search);

  useEffect(() => setPage(1), [termino, seccionId, estado]);

  const secciones = useAsync(() => estructuraApi.secciones(), []);
  const { data, loading, error, reload } = useAsync(
    () =>
      matriculasApi.listar({
        page,
        pageSize: 10,
        search: termino,
        sortBy,
        sortDir,
        seccionId: seccionId || undefined,
        estado: estado || undefined,
      }),
    [page, termino, seccionId, estado, sortBy, sortDir],
  );

  const onSort = (f: string) => {
    if (f === sortBy) setSortDir(sortDir === 'asc' ? 'desc' : 'asc');
    else {
      setSortBy(f);
      setSortDir('asc');
    }
  };

  const retirar = async (id: number, alumno: string) => {
    if (!window.confirm(`¿Retirar a ${alumno}? Se liberará la vacante y se anularán las cuotas futuras no pagadas.`)) return;
    try {
      await matriculasApi.retirar(id);
      setAviso(`${alumno} fue retirado(a).`);
      reload();
      secciones.reload();
    } catch (e) {
      window.alert(e instanceof ApiError ? e.message : 'No se pudo retirar.');
    }
  };

  return (
    <>
      <PageHeader
        title="Matrículas"
        description="Matrícula por periodo con control de vacantes y cronograma de pensiones automático."
        actions={
          <Button onClick={() => setCreando(true)}>
            <Plus className="h-4 w-4" /> Nueva matrícula
          </Button>
        }
      />
      {aviso && (
        <div className="mb-4">
          <Alert kind="success">{aviso}</Alert>
        </div>
      )}

      <Card>
        <div className="grid gap-3 border-b border-slate-200 p-4 dark:border-slate-800 md:grid-cols-[1fr_14rem_10rem]">
          <div className="relative">
            <Search className="pointer-events-none absolute left-3 top-2.5 h-4 w-4 text-slate-400" aria-hidden />
            <Input aria-label="Buscar" placeholder="Buscar alumno…" className="pl-9" value={search} onChange={(e) => setSearch(e.target.value)} />
          </div>
          <Select aria-label="Sección" value={seccionId} onChange={(e) => setSeccionId(e.target.value ? Number(e.target.value) : '')}>
            <option value="">Todas las secciones</option>
            {secciones.data?.map((s) => (
              <option key={s.id} value={s.id}>
                {s.grado} {s.letra}
              </option>
            ))}
          </Select>
          <Select aria-label="Estado" value={estado} onChange={(e) => setEstado(e.target.value as EstadoMatricula | '')}>
            <option value="">Todos</option>
            <option value="Activa">Activas</option>
            <option value="Retirada">Retiradas</option>
          </Select>
        </div>

        {loading && !data ? (
          <PageLoader />
        ) : error ? (
          <ErrorState message={error.message} onRetry={reload} />
        ) : !data || data.items.length === 0 ? (
          <EmptyState title="No hay matrículas con estos filtros" />
        ) : (
          <>
            <Table>
              <thead>
                <tr>
                  <Th>Código</Th>
                  <SortableTh label="Alumno" field="alumno" sortBy={sortBy} sortDir={sortDir} onSort={onSort} />
                  <SortableTh label="Sección" field="seccion" sortBy={sortBy} sortDir={sortDir} onSort={onSort} />
                  <SortableTh label="Fecha" field="fecha" sortBy={sortBy} sortDir={sortDir} onSort={onSort} />
                  <SortableTh label="Estado" field="estado" sortBy={sortBy} sortDir={sortDir} onSort={onSort} />
                  <Th className="text-right">Acciones</Th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100 dark:divide-slate-800">
                {data.items.map((m) => (
                  <tr key={m.id}>
                    <Td className="font-mono text-xs">{m.codigoAlumno}</Td>
                    <Td className="font-medium text-slate-900 dark:text-white">{m.alumno}</Td>
                    <Td>
                      {m.seccion} · {m.anio}
                    </Td>
                    <Td>{formatFecha(m.fechaMatricula)}</Td>
                    <Td>{m.estado === 'Activa' ? <Badge tone="green">Activa</Badge> : <Badge tone="amber">Retirada</Badge>}</Td>
                    <Td className="text-right">
                      <Link to={`/libretas/${m.id}`} className="inline-flex rounded p-1.5 text-slate-500 hover:bg-slate-100 dark:hover:bg-slate-800" aria-label="Ver libreta">
                        <NotebookText className="h-4 w-4" />
                      </Link>
                      {m.estado === 'Activa' && (
                        <Button variant="ghost" size="sm" onClick={() => retirar(m.id, m.alumno)} aria-label={`Retirar a ${m.alumno}`}>
                          <UserMinus className="h-4 w-4 text-red-500" />
                        </Button>
                      )}
                    </Td>
                  </tr>
                ))}
              </tbody>
            </Table>
            <Pagination page={data.page} totalPages={data.totalPages} totalCount={data.totalCount} pageSize={data.pageSize} onPageChange={setPage} />
          </>
        )}
      </Card>

      {creando && (
        <NuevaMatriculaModal
          onClose={() => setCreando(false)}
          onCreated={(r) => {
            setCreando(false);
            setAviso(
              `Matrícula registrada: ${r.matricula.alumno} en ${r.matricula.seccion}. Se generaron ${r.cuotasGeneradas} cuotas por ${formatMoneda(r.totalCronograma)}.`,
            );
            reload();
            secciones.reload();
          }}
        />
      )}
    </>
  );
}

function NuevaMatriculaModal({ onClose, onCreated }: { onClose: () => void; onCreated: (r: MatriculaCreada) => void }) {
  const [busqueda, setBusqueda] = useState('');
  const [alumno, setAlumno] = useState<Alumno | null>(null);
  const [seccionId, setSeccionId] = useState<number | ''>('');
  const [error, setError] = useState<string | null>(null);
  const [guardando, setGuardando] = useState(false);
  const termino = useDebounce(busqueda);

  const candidatos = useAsync(
    () => (termino.length >= 2 ? alumnosApi.listar({ search: termino, activo: true, pageSize: 8 }) : Promise.resolve(null)),
    [termino],
  );
  const secciones = useAsync(() => estructuraApi.secciones(), []);
  const seleccionada = secciones.data?.find((s) => s.id === seccionId);

  const guardar = async () => {
    if (!alumno || !seccionId) {
      setError('Seleccione un alumno y una sección.');
      return;
    }
    setGuardando(true);
    setError(null);
    try {
      onCreated(await matriculasApi.crear(alumno.id, seccionId));
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo registrar la matrícula.');
    } finally {
      setGuardando(false);
    }
  };

  return (
    <Modal
      open
      title="Nueva matrícula"
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            Cancelar
          </Button>
          <Button onClick={guardar} loading={guardando} disabled={!alumno || !seccionId}>
            Matricular
          </Button>
        </>
      }
    >
      <div className="space-y-4">
        {error && <Alert>{error}</Alert>}
        {alumno ? (
          <div className="flex items-center justify-between rounded-lg border border-slate-200 p-3 dark:border-slate-700">
            <div>
              <p className="font-medium">
                {alumno.apellidos}, {alumno.nombres}
              </p>
              <p className="text-xs text-slate-500">
                {alumno.codigo} · DNI {alumno.dni} {alumno.seccionActual && `· ya matriculado en ${alumno.seccionActual}`}
              </p>
            </div>
            <Button variant="ghost" size="sm" onClick={() => setAlumno(null)}>
              Cambiar
            </Button>
          </div>
        ) : (
          <Field label="Alumno" hint="Escriba al menos 2 letras del nombre, apellido o DNI.">
            {(id, d) => (
              <div>
                <Input id={id} aria-describedby={d} value={busqueda} onChange={(e) => setBusqueda(e.target.value)} placeholder="Buscar alumno…" />
                {candidatos.data && (
                  <ul className="mt-2 max-h-48 overflow-y-auto rounded-lg border border-slate-200 dark:border-slate-700">
                    {candidatos.data.items.length === 0 && <li className="px-3 py-2 text-sm text-slate-500">Sin resultados</li>}
                    {candidatos.data.items.map((a) => (
                      <li key={a.id}>
                        <button
                          type="button"
                          className="w-full px-3 py-2 text-left text-sm hover:bg-slate-50 dark:hover:bg-slate-800"
                          onClick={() => setAlumno(a)}
                        >
                          {a.apellidos}, {a.nombres} <span className="text-xs text-slate-500">· {a.codigo}</span>
                        </button>
                      </li>
                    ))}
                  </ul>
                )}
              </div>
            )}
          </Field>
        )}

        <Field label="Sección" hint="Las secciones sin vacantes aparecen deshabilitadas.">
          {(id, d) => (
            <Select id={id} aria-describedby={d} value={seccionId} onChange={(e) => setSeccionId(e.target.value ? Number(e.target.value) : '')}>
              <option value="">Seleccione…</option>
              {secciones.data?.map((s) => (
                <option key={s.id} value={s.id} disabled={s.vacantes === 0}>
                  {s.grado} {s.letra} — {s.vacantes} vacante(s) de {s.capacidad}
                </option>
              ))}
            </Select>
          )}
        </Field>
        {seleccionada && (
          <p className="text-sm text-slate-600 dark:text-slate-300">
            Ocupación actual: {seleccionada.matriculados}/{seleccionada.capacidad} ({seleccionada.ocupacion}%).
          </p>
        )}
      </div>
    </Modal>
  );
}
