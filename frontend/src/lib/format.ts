const soles = new Intl.NumberFormat('es-PE', { style: 'currency', currency: 'PEN', minimumFractionDigits: 2 });
const numero = new Intl.NumberFormat('es-PE');

export const formatMoneda = (valor: number): string => soles.format(valor);

export const formatNumero = (valor: number): string => numero.format(valor);

export const formatPorcentaje = (valor: number): string => `${valor.toFixed(1)} %`;

/**
 * Convierte "2026-03-31" (DateOnly del backend) en "31/03/2026".
 * Se parsea a mano: new Date("2026-03-31") se interpreta como UTC y en Lima (UTC-5)
 * mostraría el día anterior.
 */
export function formatFecha(iso: string | null | undefined): string {
  if (!iso) return '—';
  const [y, m, d] = iso.slice(0, 10).split('-');
  return d && m && y ? `${d}/${m}/${y}` : iso;
}

/** Fecha local de hoy en formato yyyy-MM-dd (para inputs type="date"). */
export function hoyIso(now: Date = new Date()): string {
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}`;
}

export function formatKpi(valor: number, formato: 'numero' | 'moneda' | 'porcentaje'): string {
  switch (formato) {
    case 'moneda':
      return formatMoneda(valor);
    case 'porcentaje':
      return formatPorcentaje(valor);
    default:
      return formatNumero(valor);
  }
}
