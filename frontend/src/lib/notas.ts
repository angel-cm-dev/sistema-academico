/**
 * Utilidades de escala vigesimal en el cliente. El cálculo oficial lo hace el backend;
 * aquí solo se usa para la VISTA PREVIA mientras el docente escribe notas, por eso
 * replica exactamente las mismas reglas (redondeo con punto medio hacia arriba).
 */

export const NOTA_MIN = 0;
export const NOTA_MAX = 20;
export const NOTA_APROBATORIA = 11;

/** Math.round ya redondea 10.5 → 11 en positivos; se acota a 0-20 como en el backend. */
export function redondearVigesimal(valor: number): number {
  const acotado = Math.min(NOTA_MAX, Math.max(NOTA_MIN, valor));
  return Math.round(acotado);
}

export function esNotaValida(valor: number): boolean {
  return Number.isFinite(valor) && valor >= NOTA_MIN && valor <= NOTA_MAX;
}

/** Promedio ponderado con las notas presentes; null si no hay ninguna. */
export function promedioPonderado(notas: { valor: number | null; peso: number }[]): number | null {
  const presentes = notas.filter((n): n is { valor: number; peso: number } => n.valor !== null && n.peso > 0);
  if (presentes.length === 0) return null;
  const sumaPesos = presentes.reduce((acc, n) => acc + n.peso, 0);
  const suma = presentes.reduce((acc, n) => acc + n.valor * n.peso, 0);
  return Math.round((suma / sumaPesos) * 100) / 100;
}

export function aprueba(promedio: number | null): boolean | null {
  return promedio === null ? null : promedio >= NOTA_APROBATORIA;
}

/** Clases de color para una nota: rojo desaprobado, verde destacado. */
export function colorNota(nota: number | null): string {
  if (nota === null) return 'text-slate-400';
  if (nota < NOTA_APROBATORIA) return 'text-red-600 dark:text-red-400 font-semibold';
  if (nota >= 18) return 'text-emerald-600 dark:text-emerald-400 font-semibold';
  return 'text-slate-800 dark:text-slate-100';
}

/** Interpreta lo que escribe el usuario: vacío → null, coma decimal aceptada. */
export function parseNota(texto: string): number | null | 'invalida' {
  const limpio = texto.trim().replace(',', '.');
  if (limpio === '') return null;
  const n = Number(limpio);
  return esNotaValida(n) ? n : 'invalida';
}
