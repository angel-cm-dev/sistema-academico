import { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { HandCoins, Search } from 'lucide-react';
import { ApiError } from '@/api/client';
import { pensionesApi } from '@/api/endpoints';
import type { Cuota, EstadoCuota } from '@/api/types';
import { useAuth } from '@/auth/AuthContext';
import { Button } from '@/components/ui/Button';
import { Alert, EmptyState, ErrorState, PageLoader } from '@/components/ui/Feedback';
import { Field, Input, Select } from '@/components/ui/Form';
import { Badge, Card, Modal, PageHeader } from '@/components/ui/Layout';
import { Pagination } from '@/components/ui/Pagination';
import { SortableTh, Table, Td, Th } from '@/components/ui/Table';
import { useAsync } from '@/hooks/useAsync';
import { useDebounce } from '@/hooks/useDebounce';
import { formatFecha, formatMoneda, hoyIso } from '@/lib/format';

const tonoEstado = { Pagada: 'green', Vencida: 'red', Pendiente: 'amber' } as const;

export default function PensionesPage() {
  const { tieneRol } = useAuth();
  const esAdmin = tieneRol('Admin');
  const [search, setSearch] = useState('');
  const [estado, setEstado] = useState<EstadoCuota | ''>(esAdmin ? 'Vencida' : '');
  const [page, setPage] = useState(1);
  const [sortBy, setSortBy] = useState('vencimiento');
  const [sortDir, setSortDir] = useState<'asc' | 'desc'>('asc');
  const [pagando, setPagando] = useState<Cuota | null>(null);
  const [aviso, setAviso] = useState<string | null>(null);
  const termino = useDebounce(search);

  useEffect(() => setPage(1), [termino, estado]);

  const { data, loading, error, reload } = useAsync(
    () => pensionesApi.listar({ page, pageSize: 10, search: termino, estado: estado || undefined, sortBy, sortDir }),
    [page, termino, estado, sortBy, sortDir],
  );

  const onSort = (f: string) => {
    if (f === sortBy) setSortDir(sortDir === 'asc' ? 'desc' : 'asc');
    else {
      setSortBy(f);
      setSortDir('asc');
    }
  };

  return (
    <>
      <PageHeader
        title="Pensiones"
        description="Cronograma de pagos con mora calculada a la fecha (0.1 % diario tras 3 días de gracia, tope 10 %)."
      />
      {aviso && (
        <div className="mb-4">
          <Alert kind="success">{aviso}</Alert>
        </div>
      )}

      <Card>
        <div className="flex flex-col gap-3 border-b border-slate-200 p-4 dark:border-slate-800 sm:flex-row">
          <div className="relative flex-1">
            <Search className="pointer-events-none absolute left-3 top-2.5 h-4 w-4 text-slate-400" aria-hidden />
            <Input aria-label="Buscar" placeholder="Buscar alumno o código…" className="pl-9" value={search} onChange={(e) => setSearch(e.target.value)} />
          </div>
          <Select aria-label="Estado" className="sm:w-44" value={estado} onChange={(e) => setEstado(e.target.value as EstadoCuota | '')}>
            <option value="">Todas</option>
            <option value="Vencida">Vencidas</option>
            <option value="Pendiente">Por vencer</option>
            <option value="Pagada">Pagadas</option>
          </Select>
        </div>

        {loading && !data ? (
          <PageLoader />
        ) : error ? (
          <ErrorState message={error.message} onRetry={reload} />
        ) : !data || data.items.length === 0 ? (
          <EmptyState title="No hay cuotas con estos filtros" />
        ) : (
          <>
            <Table>
              <thead>
                <tr>
                  <SortableTh label="Alumno" field="alumno" sortBy={sortBy} sortDir={sortDir} onSort={onSort} />
                  <Th>Concepto</Th>
                  <SortableTh label="Vence" field="vencimiento" sortBy={sortBy} sortDir={sortDir} onSort={onSort} />
                  <SortableTh label="Monto" field="monto" sortBy={sortBy} sortDir={sortDir} onSort={onSort} />
                  <Th>Mora</Th>
                  <Th>Total</Th>
                  <Th>Estado</Th>
                  {esAdmin && <Th className="text-right">Acción</Th>}
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100 dark:divide-slate-800">
                {data.items.map((c) => (
                  <tr key={c.id}>
                    <Td>
                      <p className="font-medium text-slate-900 dark:text-white">{c.alumno}</p>
                      <p className="text-xs text-slate-500">{c.seccion}</p>
                    </Td>
                    <Td>{c.concepto}</Td>
                    <Td>
                      {formatFecha(c.fechaVencimiento)}
                      {c.estado === 'Vencida' && <p className="text-xs text-red-500">{c.diasAtraso} días de atraso</p>}
                    </Td>
                    <Td>{formatMoneda(c.monto)}</Td>
                    <Td className={c.mora > 0 ? 'text-red-600 dark:text-red-400' : ''}>{formatMoneda(c.mora)}</Td>
                    <Td className="font-semibold">{formatMoneda(c.totalAPagar)}</Td>
                    <Td>
                      <Badge tone={tonoEstado[c.estado]}>{c.estado === 'Pendiente' ? 'Por vencer' : c.estado}</Badge>
                      {c.fechaPago && (
                        <p className="text-xs text-slate-500">
                          {formatFecha(c.fechaPago)} · {c.medioPago}
                        </p>
                      )}
                    </Td>
                    {esAdmin && (
                      <Td className="text-right">
                        {c.estado !== 'Pagada' && (
                          <Button size="sm" variant="secondary" onClick={() => setPagando(c)}>
                            <HandCoins className="h-3.5 w-3.5" /> Registrar pago
                          </Button>
                        )}
                      </Td>
                    )}
                  </tr>
                ))}
              </tbody>
            </Table>
            <Pagination page={data.page} totalPages={data.totalPages} totalCount={data.totalCount} pageSize={data.pageSize} onPageChange={setPage} />
          </>
        )}
      </Card>

      {pagando && (
        <PagoModal
          cuota={pagando}
          onClose={() => setPagando(null)}
          onPaid={(texto) => {
            setPagando(null);
            setAviso(texto);
            reload();
          }}
        />
      )}
    </>
  );
}

const pagoSchema = z
  .object({
    medioPago: z.enum(['Efectivo', 'Transferencia', 'Tarjeta', 'Yape']),
    numeroOperacion: z.string().trim().max(50),
    fechaPago: z.string().min(1, 'Requerida.'),
  })
  .refine((v) => v.medioPago === 'Efectivo' || v.numeroOperacion.length > 0, {
    path: ['numeroOperacion'],
    message: 'Obligatorio para pagos no presenciales.',
  })
  .refine((v) => v.fechaPago <= hoyIso(), { path: ['fechaPago'], message: 'No puede ser una fecha futura.' });
type PagoForm = z.infer<typeof pagoSchema>;

function PagoModal({ cuota, onClose, onPaid }: { cuota: Cuota; onClose: () => void; onPaid: (msg: string) => void }) {
  const [error, setError] = useState<string | null>(null);
  const {
    register,
    handleSubmit,
    watch,
    formState: { errors, isSubmitting },
  } = useForm<PagoForm>({
    resolver: zodResolver(pagoSchema),
    defaultValues: { medioPago: 'Efectivo', numeroOperacion: '', fechaPago: hoyIso() },
  });
  const medio = watch('medioPago');

  const onSubmit = async (v: PagoForm) => {
    setError(null);
    try {
      const r = await pensionesApi.pagar(cuota.id, v.medioPago, v.numeroOperacion || null, v.fechaPago);
      onPaid(`Pago registrado: ${r.cuota.concepto} de ${r.cuota.alumno} por ${formatMoneda(r.montoCobrado)}.`);
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo registrar el pago.');
    }
  };

  return (
    <Modal
      open
      title="Registrar pago"
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            Cancelar
          </Button>
          <Button type="submit" form="form-pago" loading={isSubmitting}>
            Confirmar pago
          </Button>
        </>
      }
    >
      <form id="form-pago" onSubmit={handleSubmit(onSubmit)} noValidate className="space-y-4">
        {error && <Alert>{error}</Alert>}
        <div className="rounded-lg bg-slate-50 p-3 text-sm dark:bg-slate-800">
          <p className="font-medium">{cuota.concepto}</p>
          <p className="text-slate-500">{cuota.alumno}</p>
          <p className="mt-2">
            Monto {formatMoneda(cuota.monto)} + mora a hoy {formatMoneda(cuota.mora)} ={' '}
            <strong>{formatMoneda(cuota.totalAPagar)}</strong>
          </p>
          <p className="text-xs text-slate-500">La mora definitiva se calcula con la fecha de pago y queda congelada.</p>
        </div>
        <div className="grid gap-4 sm:grid-cols-2">
          <Field label="Medio de pago" error={errors.medioPago?.message}>
            {(id) => (
              <Select id={id} {...register('medioPago')}>
                <option>Efectivo</option>
                <option>Transferencia</option>
                <option>Tarjeta</option>
                <option>Yape</option>
              </Select>
            )}
          </Field>
          <Field label="Fecha de pago" error={errors.fechaPago?.message}>
            {(id, d) => <Input id={id} aria-describedby={d} type="date" max={hoyIso()} invalid={!!errors.fechaPago} {...register('fechaPago')} />}
          </Field>
        </div>
        {medio !== 'Efectivo' && (
          <Field label="N.° de operación" error={errors.numeroOperacion?.message}>
            {(id, d) => <Input id={id} aria-describedby={d} invalid={!!errors.numeroOperacion} {...register('numeroOperacion')} />}
          </Field>
        )}
      </form>
    </Modal>
  );
}
