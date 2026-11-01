import { describe, expect, it } from 'vitest';
import { formatFecha, formatKpi, formatMoneda, hoyIso } from './format';

describe('format', () => {
  it('formatea fechas DateOnly sin desfase de zona horaria', () => {
    expect(formatFecha('2026-03-31')).toBe('31/03/2026');
    expect(formatFecha(null)).toBe('—');
  });

  it('formatea soles peruanos', () => {
    const s = formatMoneda(350);
    expect(s).toContain('350.00');
    expect(s).toMatch(/S\//);
  });

  it('formatea KPIs según su tipo', () => {
    expect(formatKpi(15.2, 'porcentaje')).toBe('15.2 %');
    expect(formatKpi(1200, 'numero')).toMatch(/1.?200/);
  });

  it('hoyIso usa la fecha local', () => {
    expect(hoyIso(new Date(2026, 0, 5, 23, 30))).toBe('2026-01-05');
  });
});
