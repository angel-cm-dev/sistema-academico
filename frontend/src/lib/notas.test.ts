import { describe, expect, it } from 'vitest';
import { aprueba, colorNota, parseNota, promedioPonderado, redondearVigesimal } from './notas';

describe('redondearVigesimal', () => {
  it.each([
    [10.5, 11],
    [10.49, 10],
    [12.5, 13],
    [19.6, 20],
    [-2, 0],
    [25, 20],
  ])('%d → %d', (valor, esperado) => {
    expect(redondearVigesimal(valor)).toBe(esperado);
  });
});

describe('promedioPonderado', () => {
  it('respeta los pesos (mismo caso que el backend)', () => {
    // (16*2 + 10 + 12 + 9) / 5 = 12.6
    const p = promedioPonderado([
      { valor: 16, peso: 2 },
      { valor: 10, peso: 1 },
      { valor: 12, peso: 1 },
      { valor: 9, peso: 1 },
    ]);
    expect(p).toBe(12.6);
    expect(redondearVigesimal(p!)).toBe(13);
  });

  it('ignora competencias sin nota en lugar de contarlas como cero', () => {
    expect(promedioPonderado([{ valor: 14, peso: 1 }, { valor: null, peso: 3 }])).toBe(14);
  });

  it('devuelve null si no hay notas', () => {
    expect(promedioPonderado([{ valor: null, peso: 1 }])).toBeNull();
  });
});

describe('parseNota', () => {
  it('acepta coma decimal y vacío', () => {
    expect(parseNota('15,5')).toBe(15.5);
    expect(parseNota('  ')).toBeNull();
  });

  it('marca como inválidas las notas fuera de escala o no numéricas', () => {
    expect(parseNota('21')).toBe('invalida');
    expect(parseNota('-1')).toBe('invalida');
    expect(parseNota('abc')).toBe('invalida');
  });
});

describe('aprueba y colorNota', () => {
  it('aprueba desde 11', () => {
    expect(aprueba(11)).toBe(true);
    expect(aprueba(10)).toBe(false);
    expect(aprueba(null)).toBeNull();
  });

  it('pinta en rojo las notas desaprobatorias', () => {
    expect(colorNota(10)).toContain('text-red');
    expect(colorNota(18)).toContain('text-emerald');
  });
});
