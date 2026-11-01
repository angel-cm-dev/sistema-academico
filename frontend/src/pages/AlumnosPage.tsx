import { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { Pencil, Plus, Search, UserX } from 'lucide-react';
import { ApiError } from '@/api/client';
import { alumnosApi, apoderadosApi } from '@/api/endpoints';
import type { Alumno, Apoderado } from '@/api/types';
import { useAuth } from '@/auth/AuthContext';
import { Button } from '@/components/ui/Button';
import { Alert, EmptyState, ErrorState, PageLoader } from '@/components/ui/Feedback';
import { Field, Input, Select } from '@/components/ui/Form';
import { Badge, Card, Modal, PageHeader } from '@/components/ui/Layout';
import { Pagination } from '@/components/ui/Pagination';
import { SortableTh, Table, Td, Th } from '@/components/ui/Table';
import { useAsync } from '@/hooks/useAsync';
import { useDebounce } from '@/hooks/useDebounce';
import { formatFecha } from '@/lib/format';

const PAGE_SIZE = 10;

export default function AlumnosPage() {
  const { tieneRol } = useAuth();
  const esAdmin = tieneRol('Admin');
  const [search, setSearch] = useState('');
  const [activo, setActivo] = useState<'todos' | 'true' | 'false'>('true');
  const [page, setPage] = useState(1);
  const [sortBy, setSortBy] = useState('apellidos');
  const [sortDir, setSortDir] = useState<'asc' | 'desc'>('asc');
  const [editando, setEditando] = useState<Alumno | 'nuevo' | null>(null);
  const termino = useDebounce(search);

  // Cualquier cambio de filtro vuelve a la página 1 (si no, podríamos pedir una página inexistente).
  useEffect(() => setPage(1), [termino, activo]);

  const { data, loading, error, reload } = useAsync(
    () =>
      alumnosApi.listar({
        page,
        pageSize: PAGE_SIZE,
        search: termino,
        sortBy,
        sortDir,
        activo: activo === 'todos' ? undefined : activo === 'true',
      }),
    [page, termino, sortBy, sortDir, activo],
  );

  const onSort = (field: string) => {
    if (field === sortBy) setSortDir(sortDir === 'asc' ? 'desc' : 'asc');
    else {
      setSortBy(field);
      setSortDir('asc');
    }
  };

  const desactivar = async (a: Alumno) => {
    if (!window.confirm(`¿Dar de baja a ${a.nombres} ${a.apellidos}? Sus notas y pagos se conservan.`)) return;
    try {
      await alumnosApi.desactivar(a.id);
      reload();
    } catch (e) {
      window.alert(e instanceof ApiError ? e.message : 'No se pudo dar de baja.');
    }
  };

  return (
    <>
      <PageHeader
        title="Alumnos"
        description={esAdmin ? 'Registro de alumnos de la institución.' : 'Alumnos vinculados a su cuenta.'}
        actions={
          esAdmin && (
            <Button onClick={() => setEditando('nuevo')}>
              <Plus className="h-4 w-4" /> Nuevo alumno
            </Button>
          )
        }
      />

      <Card>
        <div className="flex flex-col gap-3 border-b border-slate-200 p-4 dark:border-slate-800 sm:flex-row">
          <div className="relative flex-1">
            <Search className="pointer-events-none absolute left-3 top-2.5 h-4 w-4 text-slate-400" aria-hidden />
            <Input
              aria-label="Buscar alumnos"
              placeholder="Buscar por nombre, apellido, DNI o código…"
              className="pl-9"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
            />
          </div>
          <Select aria-label="Filtrar por estado" className="sm:w-44" value={activo} onChange={(e) => setActivo(e.target.value as typeof activo)}>
            <option value="true">Activos</option>
            <option value="false">Dados de baja</option>
            <option value="todos">Todos</option>
          </Select>
        </div>

        {loading && !data ? (
          <PageLoader />
        ) : error ? (
          <ErrorState message={error.message} onRetry={reload} />
        ) : data && data.items.length === 0 ? (
          <EmptyState title="No se encontraron alumnos" description="Pruebe con otro término de búsqueda o filtro." />
        ) : (
          data && (
            <>
              <Table>
                <thead>
                  <tr>
                    <SortableTh label="Código" field="codigo" sortBy={sortBy} sortDir={sortDir} onSort={onSort} />
                    <SortableTh label="Apellidos" field="apellidos" sortBy={sortBy} sortDir={sortDir} onSort={onSort} />
                    <SortableTh label="Nombres" field="nombres" sortBy={sortBy} sortDir={sortDir} onSort={onSort} />
                    <SortableTh label="DNI" field="dni" sortBy={sortBy} sortDir={sortDir} onSort={onSort} />
                    <Th>Sección</Th>
                    <Th>Apoderado</Th>
                    <SortableTh label="Nacimiento" field="fechaNacimiento" sortBy={sortBy} sortDir={sortDir} onSort={onSort} />
                    <Th>Estado</Th>
                    {esAdmin && <Th className="text-right">Acciones</Th>}
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100 dark:divide-slate-800">
                  {data.items.map((a) => (
                    <tr key={a.id} className="hover:bg-slate-50 dark:hover:bg-slate-800/40">
                      <Td className="font-mono text-xs">{a.codigo}</Td>
                      <Td className="font-medium text-slate-900 dark:text-white">{a.apellidos}</Td>
                      <Td>{a.nombres}</Td>
                      <Td>{a.dni}</Td>
                      <Td>{a.seccionActual ?? <span className="text-slate-400">Sin matrícula</span>}</Td>
                      <Td>{a.apoderado}</Td>
                      <Td>{formatFecha(a.fechaNacimiento)}</Td>
                      <Td>{a.activo ? <Badge tone="green">Activo</Badge> : <Badge>Baja</Badge>}</Td>
                      {esAdmin && (
                        <Td className="text-right">
                          <Button variant="ghost" size="sm" onClick={() => setEditando(a)} aria-label={`Editar ${a.nombres}`}>
                            <Pencil className="h-4 w-4" />
                          </Button>
                          {a.activo && (
                            <Button variant="ghost" size="sm" onClick={() => desactivar(a)} aria-label={`Dar de baja a ${a.nombres}`}>
                              <UserX className="h-4 w-4 text-red-500" />
                            </Button>
                          )}
                        </Td>
                      )}
                    </tr>
                  ))}
                </tbody>
              </Table>
              <Pagination
                page={data.page}
                totalPages={data.totalPages}
                totalCount={data.totalCount}
                pageSize={data.pageSize}
                onPageChange={setPage}
              />
            </>
          )
        )}
      </Card>

      {editando && (
        <AlumnoFormModal
          alumno={editando === 'nuevo' ? null : editando}
          onClose={() => setEditando(null)}
          onSaved={() => {
            setEditando(null);
            reload();
          }}
        />
      )}
    </>
  );
}

// ---------- Formulario ----------

const alumnoSchema = z.object({
  dni: z.string().regex(/^\d{8}$/, 'El DNI debe tener 8 dígitos.'),
  nombres: z.string().trim().min(1, 'Requerido.').max(100),
  apellidos: z.string().trim().min(1, 'Requerido.').max(100),
  fechaNacimiento: z.string().min(1, 'Requerido.'),
  apoderadoId: z.coerce.number({ invalid_type_error: 'Seleccione un apoderado.' }).int().positive('Seleccione un apoderado.'),
});
type AlumnoForm = z.infer<typeof alumnoSchema>;

function AlumnoFormModal({ alumno, onClose, onSaved }: { alumno: Alumno | null; onClose: () => void; onSaved: () => void }) {
  const [apoderados, setApoderados] = useState<Apoderado[]>([]);
  const [error, setError] = useState<string | null>(null);
  const {
    register,
    handleSubmit,
    setError: setFieldError,
    formState: { errors, isSubmitting },
  } = useForm<AlumnoForm>({
    resolver: zodResolver(alumnoSchema),
    defaultValues: alumno
      ? { dni: alumno.dni, nombres: alumno.nombres, apellidos: alumno.apellidos, fechaNacimiento: alumno.fechaNacimiento, apoderadoId: alumno.apoderadoId }
      : { dni: '', nombres: '', apellidos: '', fechaNacimiento: '', apoderadoId: 0 },
  });

  useEffect(() => {
    apoderadosApi.listar({ pageSize: 100 }).then((r) => setApoderados(r.items)).catch(() => setApoderados([]));
  }, []);

  const onSubmit = async (values: AlumnoForm) => {
    setError(null);
    try {
      if (alumno) await alumnosApi.actualizar(alumno.id, values);
      else await alumnosApi.crear(values);
      onSaved();
    } catch (e) {
      if (e instanceof ApiError) {
        // Los errores de validación del backend se muestran en su campo correspondiente.
        (Object.keys(e.errors) as (keyof AlumnoForm)[]).forEach((campo) =>
          setFieldError(campo, { message: e.fieldError(campo) }),
        );
        setError(e.message);
      } else setError('No se pudo guardar.');
    }
  };

  return (
    <Modal
      open
      title={alumno ? 'Editar alumno' : 'Nuevo alumno'}
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            Cancelar
          </Button>
          <Button type="submit" form="form-alumno" loading={isSubmitting}>
            Guardar
          </Button>
        </>
      }
    >
      <form id="form-alumno" onSubmit={handleSubmit(onSubmit)} noValidate className="space-y-4">
        {error && <Alert>{error}</Alert>}
        <div className="grid gap-4 sm:grid-cols-2">
          <Field label="Nombres" error={errors.nombres?.message}>
            {(id, d) => <Input id={id} aria-describedby={d} invalid={!!errors.nombres} {...register('nombres')} />}
          </Field>
          <Field label="Apellidos" error={errors.apellidos?.message}>
            {(id, d) => <Input id={id} aria-describedby={d} invalid={!!errors.apellidos} {...register('apellidos')} />}
          </Field>
          <Field label="DNI" error={errors.dni?.message} hint="8 dígitos">
            {(id, d) => <Input id={id} aria-describedby={d} inputMode="numeric" maxLength={8} invalid={!!errors.dni} {...register('dni')} />}
          </Field>
          <Field label="Fecha de nacimiento" error={errors.fechaNacimiento?.message}>
            {(id, d) => <Input id={id} aria-describedby={d} type="date" invalid={!!errors.fechaNacimiento} {...register('fechaNacimiento')} />}
          </Field>
        </div>
        <Field label="Apoderado" error={errors.apoderadoId?.message}>
          {(id, d) => (
            <Select id={id} aria-describedby={d} invalid={!!errors.apoderadoId} {...register('apoderadoId')}>
              <option value={0}>Seleccione…</option>
              {apoderados.map((a) => (
                <option key={a.id} value={a.id}>
                  {a.apellidos}, {a.nombres} — DNI {a.dni}
                </option>
              ))}
            </Select>
          )}
        </Field>
      </form>
    </Modal>
  );
}
