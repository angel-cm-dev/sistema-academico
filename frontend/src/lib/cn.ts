import { clsx, type ClassValue } from 'clsx';

/** Combina clases condicionales: cn('base', activo && 'activo'). */
export const cn = (...inputs: ClassValue[]) => clsx(inputs);
